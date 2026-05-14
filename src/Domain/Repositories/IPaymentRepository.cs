using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(
        long subscriptionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Payment>> GetByUserInRangeAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    Task<decimal> GetTotalAmountAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
