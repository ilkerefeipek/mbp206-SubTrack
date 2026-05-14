using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface INotificationsApi
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken ct = default);
    Task MarkReadAsync(long id, CancellationToken ct = default);
}

public sealed class NotificationsApi(HttpClient http) : ApiClientBase(http), INotificationsApi
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(CancellationToken ct = default) =>
        await GetAsync<List<NotificationDto>>("/api/notifications", ct);

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken ct = default) =>
        await GetAsync<List<NotificationDto>>("/api/notifications/unread", ct);

    public Task MarkReadAsync(long id, CancellationToken ct = default) =>
        PutAsync($"/api/notifications/{id}/read", body: null, ct);
}
