using SubTrack.Domain.Common;
using SubTrack.Domain.Enums;

namespace SubTrack.Domain.Entities;

public class Payment : BaseEntity
{
    public long SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Method { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Success;
    public string? TransactionId { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
