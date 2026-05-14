namespace SubTrack.Domain.Common;

public sealed record Insight(
    InsightType Type,
    string Title,
    string Description,
    InsightSeverity Severity,
    long? RelatedSubscriptionId = null,
    decimal? PotentialSavings = null);

public enum InsightType
{
    UnusedSubscription = 1,
    HighSpending = 2,
    UpcomingRenewal = 3,
    DuplicateService = 4
}

public enum InsightSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
