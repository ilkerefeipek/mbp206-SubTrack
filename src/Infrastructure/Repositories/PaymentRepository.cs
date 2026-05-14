using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Repositories;

public sealed class PaymentRepository(AppDbContext context)
    : Repository<Payment>(context), IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(
        long subscriptionId,
        CancellationToken ct = default) =>
        await Query()
            .Where(p => p.SubscriptionId == subscriptionId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetByUserInRangeAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) =>
        await Query()
            .Where(p => p.Subscription.UserId == userId
                && p.PaymentDate >= from
                && p.PaymentDate <= to)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

    public Task<decimal> GetTotalAmountAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) =>
        Query()
            .Where(p => p.Subscription.UserId == userId
                && p.PaymentDate >= from
                && p.PaymentDate <= to)
            .SumAsync(p => p.Amount, ct);
}
