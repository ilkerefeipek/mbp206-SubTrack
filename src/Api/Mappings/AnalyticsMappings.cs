using SubTrack.Api.Contracts.Analytics;
using SubTrack.Domain.Common;
using SubTrack.Domain.Entities;

namespace SubTrack.Api.Mappings;

public static class AnalyticsMappings
{
    public static UnusedSubscriptionDto ToUnusedDto(
        this Subscription subscription,
        string categoryName,
        DateOnly today)
    {
        var days = subscription.LastUsedDate is { } last
            ? today.DayNumber - last.DayNumber
            : int.MaxValue;

        return new UnusedSubscriptionDto(
            subscription.Id,
            subscription.Name,
            categoryName,
            subscription.Amount,
            Math.Round(BillingMath.ToMonthlyAmount(subscription.Amount, subscription.BillingCycle), 2),
            days,
            subscription.LastUsedDate);
    }

    public static InsightDto ToDto(this Insight insight) => new(
        insight.Type,
        insight.Title,
        insight.Description,
        insight.Severity,
        insight.RelatedSubscriptionId,
        insight.PotentialSavings);
}
