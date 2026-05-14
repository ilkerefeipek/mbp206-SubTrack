using FluentAssertions;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

/// <summary>
/// OWASP A05 — Security Misconfiguration. SecurityHeadersMiddleware tum response'lara
/// guvenlik header'lari ekler. Bu testler middleware'in aktif oldugunu dogrular.
/// </summary>
public class SecurityHeadersTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public SecurityHeadersTests(SubTrackWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task XContentTypeOptions_SetToNosniff()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.TryGetValues("X-Content-Type-Options", out var values).Should().BeTrue();
        values!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task XFrameOptions_SetToDeny()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.TryGetValues("X-Frame-Options", out var values).Should().BeTrue();
        values!.Single().Should().Be("DENY");
    }

    [Fact]
    public async Task ContentSecurityPolicy_SetWithDefaultSrcSelf()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.TryGetValues("Content-Security-Policy", out var values).Should().BeTrue();
        var csp = values!.Single();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("wasm-unsafe-eval"); // Blazor WASM gereksinimi
    }

    [Fact]
    public async Task ReferrerPolicy_SetToStrictOriginWhenCrossOrigin()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.TryGetValues("Referrer-Policy", out var values).Should().BeTrue();
        values!.Single().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task PermissionsPolicy_DisablesSensitiveApis()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.TryGetValues("Permissions-Policy", out var values).Should().BeTrue();
        var policy = values!.Single();
        policy.Should().Contain("geolocation=()");
        policy.Should().Contain("microphone=()");
        policy.Should().Contain("camera=()");
    }

    [Fact]
    public async Task StrictTransportSecurity_NotSentOverHttp()
    {
        // SubTrackWebAppFactory HTTP test server kullanir — HSTS sadece HTTPS'te eklenir
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
    }
}
