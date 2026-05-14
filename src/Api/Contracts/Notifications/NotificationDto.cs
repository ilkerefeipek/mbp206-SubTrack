using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Notifications;

public sealed record NotificationDto(
    long Id,
    NotificationType Type,
    string Message,
    DateTime SentAt,
    bool IsRead,
    long? SubscriptionId,
    string? Channel,
    string? Priority);
