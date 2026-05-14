using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Domain.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<IReadOnlyList<Subscription>> GetByUserAsync(long userId, CancellationToken ct = default);

    Task<PagedResult<Subscription>> GetByUserPagedAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Subscription>> GetUnusedAsync(
        long userId,
        int thresholdDays,
        CancellationToken ct = default);

    Task<IReadOnlyList<Subscription>> GetUpcomingBillingAsync(
        long userId,
        int daysAhead,
        CancellationToken ct = default);

    Task<IReadOnlyList<Subscription>> GetByCategoryAsync(
        long userId,
        long categoryId,
        CancellationToken ct = default);
}
