namespace SubTrack.Domain.Common;

/// <summary>
/// Domain-level transaction abstraction. Wraps EF Core IDbContextTransaction in
/// the Infrastructure layer so Clean Architecture dependency direction is preserved
/// (Domain has zero knowledge of EF Core types).
/// </summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
