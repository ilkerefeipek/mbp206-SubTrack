using SubTrack.Domain.Enums;

namespace SubTrack.Api.Contracts.Subscriptions;

public sealed class SubscriptionFilters
{
    public long? CategoryId { get; init; }
    public SubscriptionStatus? Status { get; init; }
    public string? Search { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
