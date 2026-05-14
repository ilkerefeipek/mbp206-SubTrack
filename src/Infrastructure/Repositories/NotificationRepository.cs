using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Repositories;

public sealed class NotificationRepository(AppDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        long userId,
        CancellationToken ct = default) =>
        await Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(
        long userId,
        CancellationToken ct = default) =>
        await Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default) =>
        Query().CountAsync(n => n.UserId == userId && !n.IsRead, ct);
}
