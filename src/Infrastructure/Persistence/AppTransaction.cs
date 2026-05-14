using Microsoft.EntityFrameworkCore.Storage;
using SubTrack.Domain.Common;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Wraps EF Core's IDbContextTransaction so the Domain layer (IAppTransaction)
/// is not coupled to Microsoft.EntityFrameworkCore types.
/// </summary>
internal sealed class AppTransaction(IDbContextTransaction inner) : IAppTransaction
{
    public Task CommitAsync(CancellationToken ct = default) => inner.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => inner.RollbackAsync(ct);
    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
