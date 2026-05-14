using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Subscriptions;

public sealed record SubscriptionUpdateRequest(
    string? Name,
    long? CategoryId,
    decimal? Amount,
    BillingCycle? BillingCycle,
    DateOnly? NextBilling,
    SubscriptionStatus? Status);
