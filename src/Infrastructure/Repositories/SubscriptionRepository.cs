using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Extensions;

namespace SubTrack.Infrastructure.Repositories;

public sealed class SubscriptionRepository(AppDbContext context)
    : Repository<Subscription>(context), ISubscriptionRepository
{
    public async Task<IReadOnlyList<Subscription>> GetByUserAsync(
        long userId,
        CancellationToken ct = default) =>
        await Query()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.NextBilling)
            .ToListAsync(ct);

    public Task<PagedResult<Subscription>> GetByUserPagedAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken ct = default) =>
        Query()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.NextBilling)
            .ToPagedResultAsync(page, pageSize, ct);

    public async Task<IReadOnlyList<Subscription>> GetUnusedAsync(
        long userId,
        int thresholdDays,
        CancellationToken ct = default)
    {
        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-thresholdDays));
        return await Query()
            .Where(s => s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && (s.LastUsedDate == null || s.LastUsedDate < thresholdDate))
            .OrderBy(s => s.LastUsedDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Subscription>> GetUpcomingBillingAsync(
        long userId,
        int daysAhead,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var until = today.AddDays(daysAhead);
        return await Query()
            .Where(s => s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && s.NextBilling >= today
                && s.NextBilling <= until)
            .OrderBy(s => s.NextBilling)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Subscription>> GetByCategoryAsync(
        long userId,
        long categoryId,
        CancellationToken ct = default) =>
        await Query()
            .Where(s => s.UserId == userId && s.CategoryId == categoryId)
            .OrderBy(s => s.NextBilling)
            .ToListAsync(ct);
}
