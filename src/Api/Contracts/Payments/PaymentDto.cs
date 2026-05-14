using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Payments;

public sealed record PaymentDto(
    long Id,
    long SubscriptionId,
    decimal Amount,
    string Currency,
    string Method,
    DateOnly PaymentDate,
    PaymentStatus Status,
    string? TransactionId,
    DateTime CreatedAt);
