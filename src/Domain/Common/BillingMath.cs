using SubTrack.Domain.Enums;

namespace SubTrack.Domain.Common;

/// <summary>
/// Pure helpers for normalizing amounts across billing cycles.
/// Used by AnalyticsService for monthly totals and category breakdowns.
/// </summary>
public static class BillingMath
{
    /// <summary>
    /// Normalize a charge to its monthly-equivalent amount.
    /// Weekly: 52 weeks per year / 12 months. Quarterly: every 3 months.
    /// Yearly: spread across 12 months.
    /// </summary>
    public static decimal ToMonthlyAmount(decimal amount, BillingCycle cycle) => cycle switch
    {
        BillingCycle.Weekly => amount * 52m / 12m,
        BillingCycle.Monthly => amount,
        BillingCycle.Quarterly => amount / 3m,
        BillingCycle.Yearly => amount / 12m,
        _ => throw new ArgumentOutOfRangeException(nameof(cycle), cycle, "Unknown billing cycle")
    };

    /// <summary>Advance the NextBilling date by one billing cycle.</summary>
    public static DateOnly AdvanceNextBilling(DateOnly current, BillingCycle cycle) => cycle switch
    {
        BillingCycle.Weekly => current.AddDays(7),
        BillingCycle.Monthly => current.AddMonths(1),
        BillingCycle.Quarterly => current.AddMonths(3),
        BillingCycle.Yearly => current.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(cycle), cycle, "Unknown billing cycle")
    };
}
