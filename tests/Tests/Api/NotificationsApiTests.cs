using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts.Notifications;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class NotificationsApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public NotificationsApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    private async Task<(HttpClient client, long userId)> AuthWithNotificationsAsync(int unread = 2, int read = 1)
    {
        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        client.WithBearer(auth.Token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = auth.User.Id;

        for (var i = 0; i < unread; i++)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.System,
                Message = $"Test unread {i}",
                IsRead = false,
                SentAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        for (var i = 0; i < read; i++)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.Welcome,
                Message = $"Test read {i}",
                IsRead = true,
                SentAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await db.SaveChangesAsync();

        return (client, userId);
    }

    [Fact]
    public async Task List_OwnedNotifications_Returns200()
    {
        var (client, _) = await AuthWithNotificationsAsync(unread: 2, read: 1);

        var items = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Unread_FiltersCorrectly()
    {
        var (client, _) = await AuthWithNotificationsAsync(unread: 2, read: 1);

        var items = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications/unread");

        items!.Should().HaveCount(2);
        items.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact]
    public async Task MarkRead_OwnedNotification_Returns204_AndFlipsFlag()
    {
        var (client, _) = await AuthWithNotificationsAsync(unread: 1, read: 0);
        var initial = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications/unread");
        var target = initial!.Single();

        var response = await client.PutAsync($"/api/notifications/{target.Id}/read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterUnread = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications/unread");
        afterUnread!.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkRead_NotOwned_Returns404()
    {
        var (clientA, _) = await AuthWithNotificationsAsync(unread: 1, read: 0);
        var clientB = _factory.CreateClientWithPartition();
        var authB = await clientB.RegisterFreshAsync();
        clientB.WithBearer(authB.Token);

        var notifA = (await clientA.GetFromJsonAsync<List<NotificationDto>>("/api/notifications/unread"))!.Single();

        var response = await clientB.PutAsync($"/api/notifications/{notifA.Id}/read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkRead_AlreadyRead_Idempotent()
    {
        var (client, userId) = await AuthWithNotificationsAsync(unread: 0, read: 1);
        var target = (await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications"))!.Single();

        var first = await client.PutAsync($"/api/notifications/{target.Id}/read", content: null);
        var second = await client.PutAsync($"/api/notifications/{target.Id}/read", content: null);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
