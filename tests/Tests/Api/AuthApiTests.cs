using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class AuthApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;

    public AuthApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    private HttpClient CreateClient(string? testClientId = null)
    {
        var client = _factory.CreateClient();
        // Per-test rate-limit partition isolation.
        client.DefaultRequestHeaders.Add(
            "X-Test-Client",
            testClientId ?? $"test-{Guid.NewGuid():N}");
        return client;
    }

    private static RegisterRequest UniqueRegister() => new(
        Email: $"u-{Guid.NewGuid():N}@example.com",
        Password: "ValidPass123",
        FirstName: "Test",
        LastName: "User");

    private async Task<AuthResponse> RegisterAsync(HttpClient client, RegisterRequest? request = null)
    {
        request ??= UniqueRegister();
        var resp = await client.PostAsJsonAsync("/api/auth/register", request);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    // ─────────────────────────────────────────────────────────────────
    // Register — TC-03 + edge cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidNewUser_Returns201WithToken()
    {
        var client = CreateClient();
        var request = UniqueRegister();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be(request.Email);
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(23));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        var client = CreateClient();
        var first = UniqueRegister();
        (await client.PostAsJsonAsync("/api/auth/register", first)).EnsureSuccessStatusCode();

        var duplicate = first with { FirstName = "Other" };
        var response = await client.PostAsJsonAsync("/api/auth/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task Register_InvalidEmail_Returns400Validation(string email)
    {
        var client = CreateClient();
        var request = UniqueRegister() with { Email = email };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400Validation()
    {
        var client = CreateClient();
        var request = UniqueRegister() with { Password = "short" };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MissingFirstName_Returns400Validation()
    {
        var client = CreateClient();
        var request = UniqueRegister() with { FirstName = "" };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_RateLimit_Returns429AfterFourthAttempt()
    {
        var client = CreateClient(testClientId: $"register-burst-{Guid.NewGuid():N}");

        for (var i = 0; i < 3; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/register", UniqueRegister());
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var fourth = await client.PostAsJsonAsync("/api/auth/register", UniqueRegister());
        fourth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ─────────────────────────────────────────────────────────────────
    // Login — TC-01 + TC-02 + edge cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken_TC01()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);
        var login = new LoginRequest(registered.User.Email, "ValidPass123");

        var response = await client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be(registered.User.Email);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401InvalidCredentials_TC02()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);
        var login = new LoginRequest(registered.User.Email, "WrongPassword!");

        var response = await client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("E-posta veya parola hatalı");
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401InvalidCredentials_NoEnumeration()
    {
        var client = CreateClient();
        var login = new LoginRequest($"ghost-{Guid.NewGuid():N}@example.com", "AnyPass123");

        var response = await client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        // Same generic message as wrong-password — no email enumeration leak.
        body.Should().Contain("E-posta veya parola hatalı");
    }

    [Fact]
    public async Task Login_MalformedEmail_Returns400Validation()
    {
        var client = CreateClient();
        var login = new LoginRequest("not-an-email", "AnyPass123");

        var response = await client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400Validation()
    {
        var client = CreateClient();
        var login = new LoginRequest("user@example.com", "");

        var response = await client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_RateLimit_Returns429AfterSixthAttempt()
    {
        var client = CreateClient(testClientId: $"login-burst-{Guid.NewGuid():N}");
        var registered = await RegisterAsync(client);

        for (var i = 0; i < 5; i++)
        {
            var resp = await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(registered.User.Email, "WrongPass123"));
            // First 5 attempts pass through to the controller (which then returns 401).
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var sixth = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(registered.User.Email, "ValidPass123"));
        sixth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_Token_ContainsExpectedClaims()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(registered.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == registered.User.Email);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Exp);
    }

    [Fact]
    public async Task Login_TokenExpiresIn24Hours()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);

        registered.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(24),
            TimeSpan.FromMinutes(2));
    }

    // ─────────────────────────────────────────────────────────────────
    // Logout — TC-10
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns200_TC10()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);

        var response = await client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_NoToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_AlreadyBlacklistedToken_Returns401_TokenRevoked()
    {
        var client = CreateClient();
        var registered = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);

        var first = await client.PostAsync("/api/auth/logout", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second logout with same (now-blacklisted) token: middleware rejects.
        var second = await client.PostAsync("/api/auth/logout", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────────────
    // Health regression (S0 surface still works)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Anonymous_Returns200_Regression()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MinimalHealth_Anonymous_Returns200_Regression()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    // ─────────────────────────────────────────────────────────────────
    // Side effects — email normalization, password hashing
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NormalizesEmailToLowercase()
    {
        var client = CreateClient();
        var email = $"MIXEDCase-{Guid.NewGuid():N}@Example.COM";
        var request = UniqueRegister() with { Email = email };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.User.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Register_StoresBcryptHashedPassword()
    {
        var client = CreateClient();
        var request = UniqueRegister();
        await client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == request.Email.ToLowerInvariant());

        user.PasswordHash.Should().NotBe(request.Password, "passwords must never be stored in cleartext");
        user.PasswordHash.Should().StartWith("$2"); // BCrypt prefix
        BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash).Should().BeTrue();
    }
}
