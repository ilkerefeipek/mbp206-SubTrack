using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class UsageLogRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetLatestAsync_Returns_MostRecent_AccessDate()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var netflix = await ctx.Subscriptions.SingleAsync(s => s.Name == "Netflix Premium");
        var repo = new UsageLogRepository(ctx);

        // Insert older log to make ordering meaningful
        await repo.AddAsync(new UsageLog
        {
            SubscriptionId = netflix.Id,
            AccessDate = DateTime.UtcNow.AddDays(-30),
            DurationMin = 10,
            Source = "web"
        });
        await ctx.SaveChangesAsync();

        var latest = await repo.GetLatestAsync(netflix.Id);

        latest.Should().NotBeNull();
        latest!.AccessDate.Should().BeAfter(DateTime.UtcNow.AddDays(-10));
    }

    [Fact]
    public async Task GetUsageCountAsync_Respects_TimeWindow()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var netflix = await ctx.Subscriptions.SingleAsync(s => s.Name == "Netflix Premium");
        var repo = new UsageLogRepository(ctx);

        var count = await repo.GetUsageCountAsync(netflix.Id, DateTime.UtcNow.AddDays(-7));

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteSubscription_CascadeRemoves_UsageLogs()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var spotify = await ctx.Subscriptions.SingleAsync(s => s.Name == "Spotify Premium");
        var spotifyId = spotify.Id;

        ctx.Subscriptions.Remove(spotify);
        await ctx.SaveChangesAsync();

        await using var verify = Fixture.CreateContext();
        var orphanLogs = await verify.UsageLogs.CountAsync(u => u.SubscriptionId == spotifyId);
        orphanLogs.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_PersistsAccessDate_Correctly()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var netflix = await ctx.Subscriptions.SingleAsync(s => s.Name == "Netflix Premium");
        var repo = new UsageLogRepository(ctx);

        var ts = DateTime.UtcNow.AddMinutes(-5);
        const int marker = 4242;
        await repo.AddAsync(new UsageLog
        {
            SubscriptionId = netflix.Id,
            AccessDate = ts,
            DurationMin = marker,
            Source = "mobile"
        });
        await ctx.SaveChangesAsync();

        await using var verify = Fixture.CreateContext();
        var roundTrip = await verify.UsageLogs.SingleAsync(u => u.DurationMin == marker);
        roundTrip.AccessDate.Should().BeCloseTo(ts, TimeSpan.FromSeconds(2));
    }
}
