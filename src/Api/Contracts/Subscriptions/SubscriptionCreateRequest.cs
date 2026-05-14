using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Subscriptions;

public sealed record SubscriptionCreateRequest(
    string Name,
    long CategoryId,
    decimal Amount,
    string? Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling);
