using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class NotificationRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetByUserAsync_Returns_All_User_Notifications()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new NotificationRepository(ctx);

        var notifs = await repo.GetByUserAsync(demoUser.Id);

        notifs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnreadByUserAsync_Filters_IsRead_False()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new NotificationRepository(ctx);

        var unread = await repo.GetUnreadByUserAsync(demoUser.Id);

        unread.Should().OnlyContain(n => !n.IsRead);
        unread.Should().HaveCount(2); // Both seeded notifications are unread
    }

    [Fact]
    public async Task GetUnreadCountAsync_Matches_Repo_Result()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new NotificationRepository(ctx);

        var count = await repo.GetUnreadCountAsync(demoUser.Id);
        var unread = await repo.GetUnreadByUserAsync(demoUser.Id);

        count.Should().Be(unread.Count);
    }

    [Fact]
    public async Task MarkRead_ChangesIsReadFlag()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new NotificationRepository(ctx);

        var firstUnread = (await repo.GetUnreadByUserAsync(demoUser.Id)).First();
        var tracked = await ctx.Notifications.FindAsync(firstUnread.Id);
        tracked!.IsRead = true;
        await ctx.SaveChangesAsync();

        var newCount = await repo.GetUnreadCountAsync(demoUser.Id);
        newCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSubscription_SetsNotificationSubscriptionId_Null_ClientSetNull()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();

        // Load both the notification AND its parent subscription into the tracker
        // ClientSetNull only fires for tracked entities.
        var netflix = await ctx.Subscriptions
            .Include(s => s.Notifications)
            .SingleAsync(s => s.Name == "Netflix Premium");

        ctx.Subscriptions.Remove(netflix);
        await ctx.SaveChangesAsync();

        await using var verify = Fixture.CreateContext();
        var orphan = await verify.Notifications
            .FirstOrDefaultAsync(n => n.Message.StartsWith("Netflix Premium"));
        orphan.Should().NotBeNull();
        orphan!.SubscriptionId.Should().BeNull();
    }
}
