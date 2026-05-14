using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SubTrack.Api.Configuration;
using SubTrack.Api.Middleware;
using SubTrack.Api.Services;
using SubTrack.Api.Validators;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// --- Controllers + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "SubTrack API", Version = "v1" });
});

// --- ProblemDetails + global exception handler ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- HttpContext accessor (AuthService uses it for client IP audit logs) ---
builder.Services.AddHttpContextAccessor();

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// --- EF Core / Infrastructure / Repositories / UoW ---
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructureRepositories();
}
else
{
    builder.Services.AddInfrastructure(builder.Configuration);
}

// --- Auth services ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<ITokenBlacklist, InMemoryTokenBlacklist>();
builder.Services.AddHostedService<TokenBlacklistCleanupService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Domain services (S3) ---
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<SubTrack.Api.Services.Email.IEmailSender, SubTrack.Api.Services.Email.NullEmailSender>();

// --- JWT bearer authentication ---
var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtConfig["Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key not configured. Run: cd src/Api && dotnet user-secrets set \"Jwt:Key\" \"<64-char random>\"");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var blacklist = ctx.HttpContext.RequestServices.GetRequiredService<ITokenBlacklist>();
                var jti = ctx.Principal?.FindFirstValue(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
                if (!string.IsNullOrEmpty(jti) && await blacklist.IsBlacklistedAsync(jti))
                {
                    ctx.Fail("Token revoked");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
const string CorsPolicy = "BlazorClient";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// --- Rate Limiter ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            title = "Cok fazla istek",
            status = 429,
            detail = "Lutfen biraz bekleyip tekrar deneyin"
        }, ct);
    };

    options.AddPolicy("login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ResolvePartitionKey(context, "login"),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                SegmentsPerWindow = 3,
                QueueLimit = 0
            }));

    options.AddPolicy("register", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ResolvePartitionKey(context, "register"),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));
});

// --- Health checks ---
builder.Services.AddHealthChecks();

var app = builder.Build();

// CLI: dotnet run --project src/Api -- --seed
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(db);
    Console.WriteLine("[seed] OK");
    return;
}

// --- Pipeline ---
app.UseExceptionHandler();
app.UseMiddleware<SubTrack.Api.Middleware.SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static string ResolvePartitionKey(HttpContext context, string policy)
{
    // Test isolation: tests can pass X-Test-Client header to get a fresh partition.
    if (context.Request.Headers.TryGetValue("X-Test-Client", out var testClient))
    {
        return $"{policy}:test:{testClient}";
    }

    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"{policy}:{ip}";
}

public partial class Program;
