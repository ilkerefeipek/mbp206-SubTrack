using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface ICategoriesApi
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
}

public sealed class CategoriesApi(HttpClient http) : ApiClientBase(http), ICategoriesApi
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default) =>
        await GetAsync<List<CategoryDto>>("/api/categories", ct);
}
