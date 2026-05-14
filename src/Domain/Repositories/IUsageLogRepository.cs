using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Domain.Repositories;

public interface IUsageLogRepository : IRepository<UsageLog>
{
    Task<UsageLog?> GetLatestAsync(long subscriptionId, CancellationToken ct = default);
    Task<int> GetUsageCountAsync(long subscriptionId, DateTime since, CancellationToken ct = default);
}
