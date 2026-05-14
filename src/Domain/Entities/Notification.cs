using SubTrack.Domain.Common;
using SubTrack.Domain.Enums;

namespace SubTrack.Domain.Entities;

public class Notification : BaseEntity
{
    public long UserId { get; set; }
    public long? SubscriptionId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string? Channel { get; set; }
    public string? Priority { get; set; }

    public User User { get; set; } = null!;
    public Subscription? Subscription { get; set; }
}
