using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts.Categories;
using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class SubscriptionsApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public SubscriptionsApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    private async Task<long> FirstCategoryIdAsync()
    {
        await _factory.EnsureCategoriesSeededAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Categories.OrderBy(c => c.SortOrder).Select(c => c.Id).FirstAsync();
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        return client.WithBearer(auth.Token);
    }

    private static object NewSubBody(long categoryId, string name = "Test Sub", decimal amount = 99.99m) => new
    {
        Name = name,
        CategoryId = categoryId,
        Amount = amount,
        Currency = "TRY",
        BillingCycle = BillingCycle.Monthly,
        NextBilling = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)).ToString("yyyy-MM-dd")
    };

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation_TC04()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();

        var response = await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var dto = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        dto!.Name.Should().Be("Test Sub");
        dto.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Create_MissingName_Returns400_TC05()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();

        var body = NewSubBody(categoryId);
        var modified = new
        {
            Name = "",
            CategoryId = categoryId,
            Amount = 50m,
            BillingCycle = BillingCycle.Monthly,
            NextBilling = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)).ToString("yyyy-MM-dd")
        };

        var response = await client.PostAsJsonAsync("/api/subscriptions", modified);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NegativeAmount_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();

        var body = NewSubBody(categoryId, amount: -10m);
        var response = await client.PostAsJsonAsync("/api/subscriptions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonExistentCategory_Returns404()
    {
        var client = await AuthenticatedClientAsync();
        await _factory.EnsureCategoriesSeededAsync();

        var body = NewSubBody(999999);
        var response = await client.PostAsJsonAsync("/api/subscriptions", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_NoAuth_Returns401()
    {
        var client = _factory.CreateClientWithPartition();
        var categoryId = await FirstCategoryIdAsync();

        var response = await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_AuthenticatedUser_ReturnsOnlyOwnedSubscriptions()
    {
        var client1 = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        await client1.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId, "U1-A"));
        await client1.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId, "U1-B"));

        var client2 = await AuthenticatedClientAsync();
        await client2.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId, "U2-A"));

        var u1List = await client1.GetFromJsonAsync<List<SubscriptionListItemDto>>("/api/subscriptions");
        var u2List = await client2.GetFromJsonAsync<List<SubscriptionListItemDto>>("/api/subscriptions");

        u1List!.Select(s => s.Name).Should().BeEquivalentTo(new[] { "U1-A", "U1-B" });
        u2List!.Select(s => s.Name).Should().BeEquivalentTo(new[] { "U2-A" });
    }

    [Fact]
    public async Task List_FilterByCategory_Returns200_TC06()
    {
        var client = await AuthenticatedClientAsync();
        await _factory.EnsureCategoriesSeededAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var streaming = await db.Categories.SingleAsync(c => c.Name == "Streaming");
        var music = await db.Categories.SingleAsync(c => c.Name == "Müzik");
        await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(streaming.Id, "Netflix"));
        await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(music.Id, "Spotify"));

        var filtered = await client.GetFromJsonAsync<List<SubscriptionListItemDto>>(
            $"/api/subscriptions?categoryId={streaming.Id}");

        filtered.Should().OnlyContain(s => s.CategoryId == streaming.Id);
        filtered.Select(s => s.Name).Should().Contain("Netflix").And.NotContain("Spotify");
    }

    [Fact]
    public async Task List_SearchByName_FiltersCorrectly()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId, "NetflixPremium"));
        await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId, "Spotify"));

        var search = await client.GetFromJsonAsync<List<SubscriptionListItemDto>>(
            "/api/subscriptions?search=netflix");

        search!.Should().HaveCount(1);
        search[0].Name.Should().Be("NetflixPremium");
    }

    [Fact]
    public async Task GetById_NotOwned_Returns404_OwnerCheck()
    {
        var client1 = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var createResp = await client1.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId));
        var sub = await createResp.Content.ReadFromJsonAsync<SubscriptionDto>();

        var client2 = await AuthenticatedClientAsync();
        var response = await client2.GetAsync($"/api/subscriptions/{sub!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.GetAsync("/api/subscriptions/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_OwnedSubscription_Returns200()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var created = await (await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId)))
            .Content.ReadFromJsonAsync<SubscriptionDto>();

        var response = await client.PutAsJsonAsync($"/api/subscriptions/{created!.Id}", new
        {
            Name = "RenamedSub",
            Status = SubscriptionStatus.Paused
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        updated!.Name.Should().Be("RenamedSub");
        updated.Status.Should().Be(SubscriptionStatus.Paused);
    }

    [Fact]
    public async Task Update_NotOwned_Returns404()
    {
        var client1 = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var sub = await (await client1.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId)))
            .Content.ReadFromJsonAsync<SubscriptionDto>();

        var client2 = await AuthenticatedClientAsync();
        var response = await client2.PutAsJsonAsync($"/api/subscriptions/{sub!.Id}", new { Name = "Hacked" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OwnedSubscription_Returns204_TC07()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var sub = await (await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId)))
            .Content.ReadFromJsonAsync<SubscriptionDto>();

        var response = await client.DeleteAsync($"/api/subscriptions/{sub!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getAfter = await client.GetAsync($"/api/subscriptions/{sub.Id}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsUsed_UpdatesLastUsedDate()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var sub = await (await client.PostAsJsonAsync("/api/subscriptions", NewSubBody(categoryId)))
            .Content.ReadFromJsonAsync<SubscriptionDto>();

        var response = await client.PostAsync($"/api/subscriptions/{sub!.Id}/mark-used", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reloaded = await client.GetFromJsonAsync<SubscriptionDto>($"/api/subscriptions/{sub.Id}");
        reloaded!.LastUsedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetUpcoming_AuthenticatedUser_Returns200WithFilteredWithinWindow()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 3 abonelik: 3 gun, 20 gun, 60 gun sonra
        async Task<SubscriptionDto> Create(string name, int daysAhead, decimal amount = 50m)
        {
            var body = new
            {
                Name = name,
                CategoryId = categoryId,
                Amount = amount,
                Currency = "TRY",
                BillingCycle = BillingCycle.Monthly,
                NextBilling = today.AddDays(daysAhead).ToString("yyyy-MM-dd")
            };
            var resp = await client.PostAsJsonAsync("/api/subscriptions", body);
            return (await resp.Content.ReadFromJsonAsync<SubscriptionDto>())!;
        }

        await Create("Yakin Abonelik", 3);
        await Create("Orta Abonelik", 20);
        await Create("Uzak Abonelik", 60);

        // daysAhead=7 -> sadece 3 gun sonraki gelmeli
        var response = await client.GetAsync("/api/subscriptions/upcoming?daysAhead=7");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await response.Content.ReadFromJsonAsync<List<SubscriptionListItemDto>>())!;
        list.Should().Contain(s => s.Name == "Yakin Abonelik");
        list.Should().NotContain(s => s.Name == "Orta Abonelik");
        list.Should().NotContain(s => s.Name == "Uzak Abonelik");
    }

    [Fact]
    public async Task GetUpcoming_DefaultDaysAhead_Returns7DayWindow()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = await FirstCategoryIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var bodyToday = new
        {
            Name = "Bugun Yenilenen",
            CategoryId = categoryId,
            Amount = 99m,
            Currency = "TRY",
            BillingCycle = BillingCycle.Monthly,
            NextBilling = today.ToString("yyyy-MM-dd")
        };
        await client.PostAsJsonAsync("/api/subscriptions", bodyToday);

        // Query string olmadan default daysAhead=7 olmali
        var response = await client.GetAsync("/api/subscriptions/upcoming");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await response.Content.ReadFromJsonAsync<List<SubscriptionListItemDto>>())!;
        list.Should().Contain(s => s.Name == "Bugun Yenilenen");
    }
}
