using SubTrack.Domain.Repositories;

namespace SubTrack.Domain.Common;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    ISubscriptionRepository Subscriptions { get; }
    IPaymentRepository Payments { get; }
    INotificationRepository Notifications { get; }
    IUsageLogRepository UsageLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
