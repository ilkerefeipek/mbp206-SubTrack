using SubTrack.Api.Contracts.Payments;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class PaymentMappings
{
    public static PaymentDto ToDto(this Payment p) => new(
        p.Id,
        p.SubscriptionId,
        p.Amount,
        p.Currency,
        p.Method,
        p.PaymentDate,
        p.Status,
        p.TransactionId,
        p.CreatedAt);
}
