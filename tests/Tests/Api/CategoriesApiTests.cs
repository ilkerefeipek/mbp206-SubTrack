using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SubTrack.Api.Contracts.Categories;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class CategoriesApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public CategoriesApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task List_AuthenticatedUser_ReturnsDefaultCategories()
    {
        await _factory.EnsureCategoriesSeededAsync();
        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        client.WithBearer(auth.Token);

        var response = await client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterThanOrEqualTo(5);
        items.Select(c => c.Name).Should().Contain(new[] { "Streaming", "Muzik", "Verimlilik" });
    }

    [Fact]
    public async Task List_NoAuth_Returns401()
    {
        var client = _factory.CreateClientWithPartition();

        var response = await client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_OrderedBySortOrder()
    {
        await _factory.EnsureCategoriesSeededAsync();
        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        client.WithBearer(auth.Token);

        var items = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        items!.Select(c => c.SortOrder).Should().BeInAscendingOrder();
    }
}
