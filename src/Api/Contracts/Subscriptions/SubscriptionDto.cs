using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Subscriptions;

public sealed record SubscriptionDto(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling,
    DateOnly? LastUsedDate,
    SubscriptionStatus Status,
    DateTime CreatedAt);

public sealed record SubscriptionListItemDto(
    long Id,
    string Name,
    long CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    BillingCycle BillingCycle,
    DateOnly NextBilling,
    DateOnly? LastUsedDate,
    SubscriptionStatus Status);
