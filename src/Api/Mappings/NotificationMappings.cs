using SubTrack.Api.Contracts.Notifications;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification n) => new(
        n.Id,
        n.Type,
        n.Message,
        n.SentAt,
        n.IsRead,
        n.SubscriptionId,
        n.Channel,
        n.Priority);
}
