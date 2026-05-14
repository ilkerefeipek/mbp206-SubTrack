using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class SubscriptionRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetByUserAsync_Returns_Only_That_Users_Subscriptions()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new SubscriptionRepository(ctx);

        var subs = await repo.GetByUserAsync(demoUser.Id);

        subs.Should().HaveCount(7);
        subs.Should().OnlyContain(s => s.UserId == demoUser.Id);
    }

    [Fact]
    public async Task GetByUserAsync_Empty_For_Unknown_User()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new SubscriptionRepository(ctx);

        var subs = await repo.GetByUserAsync(userId: 999999);

        subs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserPagedAsync_Respects_Page_And_PageSize()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new SubscriptionRepository(ctx);

        var page1 = await repo.GetByUserPagedAsync(demoUser.Id, page: 1, pageSize: 3);

        page1.Items.Should().HaveCount(3);
        page1.TotalCount.Should().Be(7);
        page1.TotalPages.Should().Be(3);
        page1.HasNext.Should().BeTrue();
        page1.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnusedAsync_Respects_Threshold_And_Excludes_Active_RecentUse()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new SubscriptionRepository(ctx);

        // Seed has Disney+ LastUsedDate today-45 days (unused @ threshold 30)
        // Adobe LastUsedDate today-60 days (unused @ threshold 30)
        // Netflix today-2 days (in use)
        var unused = await repo.GetUnusedAsync(demoUser.Id, thresholdDays: 30);

        unused.Select(s => s.Name).Should()
            .Contain(new[] { "Disney+", "Adobe Creative Cloud" })
            .And.NotContain("Netflix Premium");
    }

    [Fact]
    public async Task GetUnusedAsync_Excludes_Cancelled_Subscriptions()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var streaming = await ctx.Categories.SingleAsync(c => c.Name == "Streaming");
        var repo = new SubscriptionRepository(ctx);

        var cancelled = MakeSubscription(demoUser.Id, streaming.Id,
            name: "Old Cancelled",
            status: SubscriptionStatus.Cancelled,
            lastUsed: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)));
        await repo.AddAsync(cancelled);
        await ctx.SaveChangesAsync();

        var unused = await repo.GetUnusedAsync(demoUser.Id, thresholdDays: 30);

        unused.Should().NotContain(s => s.Name == "Old Cancelled");
    }

    [Fact]
    public async Task GetUpcomingBillingAsync_Returns_Active_Within_Window()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new SubscriptionRepository(ctx);

        // Seed: YouTube +3 days, Netflix +7, GitHub Pro +11, Spotify +14, Disney+ +21 → within 30 day window
        var upcoming = await repo.GetUpcomingBillingAsync(demoUser.Id, daysAhead: 30);

        upcoming.Should().NotBeEmpty();
        upcoming.Select(s => s.Name).Should().Contain(new[] { "YouTube Premium", "Netflix Premium" });
    }

    [Fact]
    public async Task GetByCategoryAsync_Filters_Correctly()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var streaming = await ctx.Categories.SingleAsync(c => c.Name == "Streaming");
        var repo = new SubscriptionRepository(ctx);

        var streamingSubs = await repo.GetByCategoryAsync(demoUser.Id, streaming.Id);

        streamingSubs.Should().HaveCount(3);
        streamingSubs.Should().OnlyContain(s => s.CategoryId == streaming.Id);
    }

    [Fact]
    public async Task AddAsync_NegativeAmount_Throws_DueTo_CheckConstraint()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var streaming = await ctx.Categories.SingleAsync(c => c.Name == "Streaming");
        var repo = new SubscriptionRepository(ctx);

        await repo.AddAsync(MakeSubscription(demoUser.Id, streaming.Id, amount: -10m));

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
