using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Domain.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(long userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
}
