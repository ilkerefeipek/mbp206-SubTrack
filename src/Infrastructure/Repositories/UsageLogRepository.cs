using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Repositories;

public sealed class UsageLogRepository(AppDbContext context)
    : Repository<UsageLog>(context), IUsageLogRepository
{
    public Task<UsageLog?> GetLatestAsync(long subscriptionId, CancellationToken ct = default) =>
        Query()
            .Where(u => u.SubscriptionId == subscriptionId)
            .OrderByDescending(u => u.AccessDate)
            .FirstOrDefaultAsync(ct);

    public Task<int> GetUsageCountAsync(
        long subscriptionId,
        DateTime since,
        CancellationToken ct = default) =>
        Query().CountAsync(u => u.SubscriptionId == subscriptionId && u.AccessDate >= since, ct);
}
