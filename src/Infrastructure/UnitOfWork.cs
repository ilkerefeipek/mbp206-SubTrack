using SubTrack.Domain.Common;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure;

public sealed class UnitOfWork(
    AppDbContext context,
    IUserRepository users,
    ICategoryRepository categories,
    ISubscriptionRepository subscriptions,
    IPaymentRepository payments,
    INotificationRepository notifications,
    IUsageLogRepository usageLogs) : IUnitOfWork
{
    public IUserRepository Users { get; } = users;
    public ICategoryRepository Categories { get; } = categories;
    public ISubscriptionRepository Subscriptions { get; } = subscriptions;
    public IPaymentRepository Payments { get; } = payments;
    public INotificationRepository Notifications { get; } = notifications;
    public IUsageLogRepository UsageLogs { get; } = usageLogs;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var inner = await context.Database.BeginTransactionAsync(ct);
        return new AppTransaction(inner);
    }

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
