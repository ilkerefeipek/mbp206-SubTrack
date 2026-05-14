using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface IAnalyticsApi
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategoryBreakdownItemDto>> GetBreakdownAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyTrendItemDto>> GetTrendAsync(int months, CancellationToken ct = default);
    Task<IReadOnlyList<UnusedSubscriptionDto>> GetUnusedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InsightDto>> GetInsightsAsync(CancellationToken ct = default);
}

public sealed class AnalyticsApi(HttpClient http) : ApiClientBase(http), IAnalyticsApi
{
    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default) =>
        GetAsync<DashboardSummaryDto>("/api/analytics/summary", ct);

    public async Task<IReadOnlyList<CategoryBreakdownItemDto>> GetBreakdownAsync(CancellationToken ct = default) =>
        await GetAsync<List<CategoryBreakdownItemDto>>("/api/analytics/breakdown", ct);

    public async Task<IReadOnlyList<MonthlyTrendItemDto>> GetTrendAsync(int months, CancellationToken ct = default) =>
        await GetAsync<List<MonthlyTrendItemDto>>($"/api/analytics/trend?months={months}", ct);

    public async Task<IReadOnlyList<UnusedSubscriptionDto>> GetUnusedAsync(CancellationToken ct = default) =>
        await GetAsync<List<UnusedSubscriptionDto>>("/api/analytics/unused", ct);

    public async Task<IReadOnlyList<InsightDto>> GetInsightsAsync(CancellationToken ct = default) =>
        await GetAsync<List<InsightDto>>("/api/analytics/insights", ct);
}
