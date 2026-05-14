using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Common;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Repositories;

public abstract class Repository<TEntity>(AppDbContext context) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    /// <summary>AsNoTracking query for read paths (default for repository reads).</summary>
    protected IQueryable<TEntity> Query() => _dbSet.AsNoTracking();

    /// <summary>Tracked query — use only when entity will be mutated and saved.</summary>
    protected IQueryable<TEntity> TrackedQuery() => _dbSet;

    public virtual Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Query().FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default) =>
        await Query().ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default) =>
        await Query().Where(predicate).ToListAsync(ct);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual void Update(TEntity entity) =>
        _context.Entry(entity).State = EntityState.Modified;

    public virtual void Remove(TEntity entity) => _dbSet.Remove(entity);

    public virtual Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default) =>
        Query().AnyAsync(predicate, ct);

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default) =>
        predicate is null
            ? Query().CountAsync(ct)
            : Query().CountAsync(predicate, ct);
}
