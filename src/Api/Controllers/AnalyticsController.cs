using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Api.Contracts.Analytics;
using SubTrack.Api.Services;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public sealed class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await analyticsService.GetSummaryAsync(ct));

    [HttpGet("breakdown")]
    public async Task<ActionResult<IReadOnlyList<CategoryBreakdownItemDto>>> Breakdown(CancellationToken ct) =>
        Ok(await analyticsService.GetBreakdownAsync(ct));

    [HttpGet("trend")]
    public async Task<ActionResult<IReadOnlyList<MonthlyTrendItemDto>>> Trend(
        [FromQuery] int months,
        CancellationToken ct) =>
        Ok(await analyticsService.GetTrendAsync(months, ct));

    [HttpGet("unused")]
    public async Task<ActionResult<IReadOnlyList<UnusedSubscriptionDto>>> Unused(CancellationToken ct) =>
        Ok(await analyticsService.GetUnusedAsync(ct));

    [HttpGet("insights")]
    public async Task<ActionResult<IReadOnlyList<InsightDto>>> Insights(CancellationToken ct) =>
        Ok(await analyticsService.GetInsightsAsync(ct));
}
