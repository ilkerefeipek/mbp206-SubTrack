using SubTrack.Domain.Common;

namespace SubTrack.Api.Contracts.Analytics;

public sealed record DashboardSummaryDto(
    int ActiveCount,
    decimal MonthlyTotal,
    string Currency,
    int UpcomingCount,
    int UnusedCount);

public sealed record CategoryBreakdownItemDto(
    long CategoryId,
    string CategoryName,
    decimal MonthlyAmount,
    decimal Percentage,
    int SubscriptionCount);

public sealed record MonthlyTrendItemDto(
    int Year,
    int Month,
    decimal Amount,
    string Currency);

public sealed record UnusedSubscriptionDto(
    long SubscriptionId,
    string Name,
    string CategoryName,
    decimal Amount,
    decimal MonthlyAmount,
    int DaysSinceLastUse,
    DateOnly? LastUsedDate);

public sealed record InsightDto(
    InsightType Type,
    string Title,
    string Description,
    InsightSeverity Severity,
    long? RelatedSubscriptionId,
    decimal? PotentialSavings);
