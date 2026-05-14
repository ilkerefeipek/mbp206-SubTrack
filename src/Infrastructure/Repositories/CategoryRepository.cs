using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Repositories;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext context)
    : Repository<Category>(context), ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetDefaultsAsync(CancellationToken ct = default) =>
        await Query()
            .Where(c => c.IsDefault)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public Task<Category?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Query().FirstOrDefaultAsync(c => c.Name == name, ct);
}
