using SubTrack.Domain.Common;
using SubTrack.Domain.Enums;

namespace SubTrack.Domain.Entities;

public class Subscription : BaseEntity
{
    public long UserId { get; set; }
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public DateOnly NextBilling { get; set; }
    public DateOnly? LastUsedDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
