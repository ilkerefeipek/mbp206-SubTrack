using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Domain.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetDefaultsAsync(CancellationToken ct = default);
    Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);
}
