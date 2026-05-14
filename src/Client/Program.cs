using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SubTrack.Client;
using SubTrack.Client.Services.Api;
using SubTrack.Client.Services.Auth;
using SubTrack.Client.Services.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- Local storage + authorization ---
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// --- Toast notifications ---
builder.Services.AddScoped<IToastService, ToastService>();

// --- HTTP client with JWT bearer attach + 401 redirect ---
builder.Services.AddTransient<AuthorizationMessageHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

builder.Services
    .AddHttpClient("SubTrackApi", c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SubTrackApi"));

// --- API client services ---
builder.Services.AddScoped<IAuthApi, AuthApi>();
builder.Services.AddScoped<ICategoriesApi, CategoriesApi>();
builder.Services.AddScoped<ISubscriptionsApi, SubscriptionsApi>();
builder.Services.AddScoped<IPaymentsApi, PaymentsApi>();
builder.Services.AddScoped<IAnalyticsApi, AnalyticsApi>();
builder.Services.AddScoped<INotificationsApi, NotificationsApi>();

await builder.Build().RunAsync();
