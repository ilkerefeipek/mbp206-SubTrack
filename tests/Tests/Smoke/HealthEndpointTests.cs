using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Smoke;

public class HealthEndpointTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;

    public HealthEndpointTests(SubTrackWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetApiHealth_Returns_200_With_OkStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("OK");
        payload.Environment.Should().Be("Testing");
        payload.DbConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetHealth_Returns_Healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    private sealed record HealthResponse(string Status, string Version, string Environment, bool DbConnected);
}
