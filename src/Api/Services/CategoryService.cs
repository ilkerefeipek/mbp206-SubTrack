using SubTrack.Api.Contracts.Categories;
using SubTrack.Api.Mappings;
using SubTrack.Domain.Common;

namespace SubTrack.Api.Services;

public sealed class CategoryService(IUnitOfWork uow) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default)
    {
        var all = await uow.Categories.ListAsync(ct);
        return all
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToList();
    }
}
