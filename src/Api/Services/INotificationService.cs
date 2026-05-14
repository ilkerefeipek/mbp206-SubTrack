using SubTrack.Api.Contracts.Notifications;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Services;

public interface INotificationService
{
    Task SendRenewalReminderAsync(Subscription subscription, CancellationToken ct = default);
    Task SendUnusedAlertAsync(Subscription subscription, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetByUserAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken ct = default);
    Task MarkReadAsync(long id, CancellationToken ct = default);
}
