using SubTrack.Api.Contracts.Notifications;
using SubTrack.Api.Mappings;
using SubTrack.Api.Services.Email;
using SubTrack.Domain.Common;
using SubTrack.Domain.Common.Exceptions;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;

namespace SubTrack.Api.Services;

public sealed class NotificationService(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IEmailSender emailSender,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendRenewalReminderAsync(Subscription subscription, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(subscription.UserId, ct)
            ?? throw new EntityNotFoundException("User", subscription.UserId);

        var notification = new Notification
        {
            UserId = subscription.UserId,
            SubscriptionId = subscription.Id,
            Type = NotificationType.RenewalReminder,
            Message = $"{subscription.Name} aboneliginiz {subscription.NextBilling:yyyy-MM-dd} tarihinde yenilenecek.",
            Channel = "in-app",
            Priority = "normal",
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await uow.Notifications.AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                $"Yenileme hatirlatmasi: {subscription.Name}",
                notification.Message),
            ct);

        logger.LogInformation(
            "Renewal reminder created for user {UserId}, subscription {SubscriptionId}",
            subscription.UserId,
            subscription.Id);
    }

    public async Task SendUnusedAlertAsync(Subscription subscription, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(subscription.UserId, ct)
            ?? throw new EntityNotFoundException("User", subscription.UserId);

        var notification = new Notification
        {
            UserId = subscription.UserId,
            SubscriptionId = subscription.Id,
            Type = NotificationType.UnusedAlert,
            Message = $"{subscription.Name} aboneliginiz son {user.ThresholdDays} gunden uzun suredir kullanilmadi.",
            Channel = "email",
            Priority = "high",
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await uow.Notifications.AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                $"Kullanilmayan abonelik: {subscription.Name}",
                notification.Message),
            ct);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByUserAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var notifications = await uow.Notifications.GetByUserAsync(userId, ct);
        return notifications.Select(n => n.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var notifications = await uow.Notifications.GetUnreadByUserAsync(userId, ct);
        return notifications.Select(n => n.ToDto()).ToList();
    }

    public async Task MarkReadAsync(long id, CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var notification = await uow.Notifications.GetByIdAsync(id, ct);

        if (notification is null || notification.UserId != userId)
        {
            throw new EntityNotFoundException("Notification", id);
        }

        if (notification.IsRead)
        {
            return; // idempotent
        }

        notification.IsRead = true;
        uow.Notifications.Update(notification);
        await uow.SaveChangesAsync(ct);
    }
}
