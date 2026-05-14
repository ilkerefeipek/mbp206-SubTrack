using SubTrack.Api.Contracts.Subscriptions;

namespace SubTrack.Api.Services;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(SubscriptionFilters filters, CancellationToken ct = default);
    Task<SubscriptionDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SubscriptionDto> CreateAsync(SubscriptionCreateRequest request, CancellationToken ct = default);
    Task<SubscriptionDto> UpdateAsync(long id, SubscriptionUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task MarkAsUsedAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionListItemDto>> GetUpcomingAsync(int daysAhead, CancellationToken ct = default);
}
