using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts.Analytics;
using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Domain.Common;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class AnalyticsApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public AnalyticsApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> NewAuthClientAsync()
    {
        await _factory.EnsureCategoriesSeededAsync();
        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        return client.WithBearer(auth.Token);
    }

    private static object NewSub(long catId, decimal amount, BillingCycle cycle, int daysOffset = 15) => new
    {
        Name = $"Sub-{Guid.NewGuid():N}",
        CategoryId = catId,
        Amount = amount,
        BillingCycle = cycle,
        NextBilling = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysOffset)).ToString("yyyy-MM-dd")
    };

    private async Task<long> AnyCategoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Categories.OrderBy(c => c.SortOrder).Select(c => c.Id).FirstAsync();
    }

    [Fact]
    public async Task Summary_AuthenticatedUser_Returns200_TC08()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 100m, BillingCycle.Monthly));

        var response = await client.GetAsync("/api/analytics/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary!.ActiveCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Summary_MonthlyTotal_UsesBillingMath()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();
        // Yearly 1200 → ~100/month. Monthly 50 → 50. Total ~150.
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 1200m, BillingCycle.Yearly));
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 50m, BillingCycle.Monthly));

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/analytics/summary");

        summary!.MonthlyTotal.Should().BeApproximately(150m, 0.01m);
        summary.ActiveCount.Should().Be(2);
    }

    [Fact]
    public async Task Summary_OnlyCountsActiveSubscriptions()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();
        var create1 = await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 100m, BillingCycle.Monthly));
        var sub1 = await create1.Content.ReadFromJsonAsync<SubscriptionDto>();
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 200m, BillingCycle.Monthly));

        await client.PutAsJsonAsync($"/api/subscriptions/{sub1!.Id}", new { Status = SubscriptionStatus.Cancelled });

        var summary = await client.GetFromJsonAsync<DashboardSummaryDto>("/api/analytics/summary");
        summary!.ActiveCount.Should().Be(1);
        summary.MonthlyTotal.Should().BeApproximately(200m, 0.01m);
    }

    [Fact]
    public async Task Breakdown_Empty_For_NoSubscriptions()
    {
        var client = await NewAuthClientAsync();
        var items = await client.GetFromJsonAsync<List<CategoryBreakdownItemDto>>("/api/analytics/breakdown");
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task Breakdown_PercentagesSum_To_100()
    {
        var client = await NewAuthClientAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var streaming = await db.Categories.SingleAsync(c => c.Name == "Streaming");
        var music = await db.Categories.SingleAsync(c => c.Name == "Muzik");
        var productivity = await db.Categories.SingleAsync(c => c.Name == "Verimlilik");

        await client.PostAsJsonAsync("/api/subscriptions", NewSub(streaming.Id, 50m, BillingCycle.Monthly));
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(music.Id, 30m, BillingCycle.Monthly));
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(productivity.Id, 100m, BillingCycle.Monthly));

        var items = await client.GetFromJsonAsync<List<CategoryBreakdownItemDto>>("/api/analytics/breakdown");

        items.Should().HaveCount(3);
        items!.Sum(i => i.Percentage).Should().Be(100m);
    }

    [Fact]
    public async Task Trend_ReturnsExpectedMonthCount_AndOrderedAscending()
    {
        var client = await NewAuthClientAsync();

        var items = await client.GetFromJsonAsync<List<MonthlyTrendItemDto>>("/api/analytics/trend?months=6");

        items.Should().HaveCount(6);
        // Ordered ascending: oldest first
        for (var i = 1; i < items!.Count; i++)
        {
            var prev = new DateOnly(items[i - 1].Year, items[i - 1].Month, 1);
            var curr = new DateOnly(items[i].Year, items[i].Month, 1);
            curr.Should().BeAfter(prev);
        }
    }

    [Fact]
    public async Task Trend_FillsMissingMonths_WithZero()
    {
        var client = await NewAuthClientAsync();

        // No subs, no payments → all months should have 0 amount.
        var items = await client.GetFromJsonAsync<List<MonthlyTrendItemDto>>("/api/analytics/trend?months=3");

        items!.Should().HaveCount(3);
        items.Should().OnlyContain(i => i.Amount == 0m);
    }

    [Fact]
    public async Task Trend_Months_CappedAt_24()
    {
        var client = await NewAuthClientAsync();
        var items = await client.GetFromJsonAsync<List<MonthlyTrendItemDto>>("/api/analytics/trend?months=999");
        items!.Should().HaveCount(24);
    }

    [Fact]
    public async Task Unused_AuthenticatedUser_Returns200_TC09()
    {
        var client = await NewAuthClientAsync();
        var response = await client.GetAsync("/api/analytics/unused");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unused_RespectsThresholdDays()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();

        // New subscription with no LastUsedDate → counts as unused immediately
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 50m, BillingCycle.Monthly));

        var unused = await client.GetFromJsonAsync<List<UnusedSubscriptionDto>>("/api/analytics/unused");
        unused.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Unused_ExcludesRecentlyUsed()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();
        var resp = await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 50m, BillingCycle.Monthly));
        var sub = await resp.Content.ReadFromJsonAsync<SubscriptionDto>();
        await client.PostAsync($"/api/subscriptions/{sub!.Id}/mark-used", null);

        var unused = await client.GetFromJsonAsync<List<UnusedSubscriptionDto>>("/api/analytics/unused");
        unused.Should().NotContain(u => u.SubscriptionId == sub.Id);
    }

    [Fact]
    public async Task Insights_IncludesHighSpendingWhenTotalExceedsThreshold()
    {
        var client = await NewAuthClientAsync();
        var catId = await AnyCategoryAsync();
        // 600 TRY/month >> 500 TRY threshold
        await client.PostAsJsonAsync("/api/subscriptions", NewSub(catId, 600m, BillingCycle.Monthly));

        var insights = await client.GetFromJsonAsync<List<InsightDto>>("/api/analytics/insights");

        insights.Should().Contain(i => i.Type == InsightType.HighSpending);
    }

    [Fact]
    public async Task Insights_NoAuth_Returns401()
    {
        var client = _factory.CreateClientWithPartition();
        var response = await client.GetAsync("/api/analytics/insights");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
