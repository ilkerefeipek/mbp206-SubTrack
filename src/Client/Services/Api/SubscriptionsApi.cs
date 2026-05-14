using System.Web;
using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface ISubscriptionsApi
{
    Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(SubscriptionFilters? filters, CancellationToken ct = default);
    Task<SubscriptionDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SubscriptionDto> CreateAsync(SubscriptionCreateRequest request, CancellationToken ct = default);
    Task<SubscriptionDto> UpdateAsync(long id, SubscriptionUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task MarkAsUsedAsync(long id, CancellationToken ct = default);
}

public sealed class SubscriptionsApi(HttpClient http) : ApiClientBase(http), ISubscriptionsApi
{
    public async Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(
        SubscriptionFilters? filters,
        CancellationToken ct = default)
    {
        var query = BuildQueryString(filters);
        return await GetAsync<List<SubscriptionListItemDto>>($"/api/subscriptions{query}", ct);
    }

    public Task<SubscriptionDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/subscriptions/{id}", ct);

    public Task<SubscriptionDto> CreateAsync(SubscriptionCreateRequest request, CancellationToken ct = default) =>
        PostAsync<SubscriptionDto>("/api/subscriptions", request, ct);

    public Task<SubscriptionDto> UpdateAsync(
        long id,
        SubscriptionUpdateRequest request,
        CancellationToken ct = default) =>
        PutAsync<SubscriptionDto>($"/api/subscriptions/{id}", request, ct);

    public Task DeleteAsync(long id, CancellationToken ct = default) =>
        DeleteAsync($"/api/subscriptions/{id}", ct);

    public Task MarkAsUsedAsync(long id, CancellationToken ct = default) =>
        PostAsync($"/api/subscriptions/{id}/mark-used", body: null, ct);

    private static string BuildQueryString(SubscriptionFilters? f)
    {
        if (f is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (f.CategoryId.HasValue)
        {
            parts.Add($"categoryId={f.CategoryId.Value}");
        }

        if (f.Status.HasValue)
        {
            parts.Add($"status={f.Status.Value}");
        }

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            parts.Add($"search={HttpUtility.UrlEncode(f.Search)}");
        }

        if (f.Page.HasValue)
        {
            parts.Add($"page={f.Page.Value}");
        }

        if (f.PageSize.HasValue)
        {
            parts.Add($"pageSize={f.PageSize.Value}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
