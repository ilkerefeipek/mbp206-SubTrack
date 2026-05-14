using SubTrack.Api.Contracts.Analytics;

namespace SubTrack.Api.Services;

public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategoryBreakdownItemDto>> GetBreakdownAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyTrendItemDto>> GetTrendAsync(int months, CancellationToken ct = default);
    Task<IReadOnlyList<UnusedSubscriptionDto>> GetUnusedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InsightDto>> GetInsightsAsync(CancellationToken ct = default);
}
