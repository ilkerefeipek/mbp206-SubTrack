using SubTrack.Api.Contracts.Categories;

namespace SubTrack.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
}
