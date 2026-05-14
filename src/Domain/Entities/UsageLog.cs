using SubTrack.Domain.Common;

namespace SubTrack.Domain.Entities;

public class UsageLog : BaseEntity
{
    public long SubscriptionId { get; set; }
    public DateTime AccessDate { get; set; } = DateTime.UtcNow;
    public int? DurationMin { get; set; }
    public string? Source { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
