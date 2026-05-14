using SubTrack.Api.Contracts.Analytics;
using SubTrack.Api.Mappings;
using SubTrack.Domain.Common;
using SubTrack.Domain.Common.Exceptions;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;

namespace SubTrack.Api.Services;

public sealed class AnalyticsService(
    IUnitOfWork uow,
    ICurrentUserService currentUser) : IAnalyticsService
{
    private const decimal _highSpendingThresholdTry = 500m;
    private const int _upcomingDays = 3;
    private const int _duplicateCategoryThreshold = 3;
    private const int _maxTrendMonths = 24;
    private const int _defaultTrendMonths = 12;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var (user, active) = await LoadUserAndActiveAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var monthlyTotal = active.Sum(s => BillingMath.ToMonthlyAmount(s.Amount, s.BillingCycle));
        var upcoming = active.Count(s => s.NextBilling <= today.AddDays(7));
        var unused = active.Count(s => IsUnused(s, user.ThresholdDays, today));

        return new DashboardSummaryDto(
            ActiveCount: active.Count,
            MonthlyTotal: Math.Round(monthlyTotal, 2),
            Currency: user.PreferredCurrency,
            UpcomingCount: upcoming,
            UnusedCount: unused);
    }

    public async Task<IReadOnlyList<CategoryBreakdownItemDto>> GetBreakdownAsync(CancellationToken ct = default)
    {
        var (_, active) = await LoadUserAndActiveAsync(ct);
        if (active.Count == 0)
        {
            return Array.Empty<CategoryBreakdownItemDto>();
        }

        var categories = (await uow.Categories.ListAsync(ct)).ToDictionary(c => c.Id);
        var total = active.Sum(s => BillingMath.ToMonthlyAmount(s.Amount, s.BillingCycle));

        var items = active
            .GroupBy(s => s.CategoryId)
            .Select(g =>
            {
                var monthly = Math.Round(
                    g.Sum(s => BillingMath.ToMonthlyAmount(s.Amount, s.BillingCycle)),
                    2);
                var pct = total > 0
                    ? Math.Round(monthly / total * 100m, 1)
                    : 0m;
                return new CategoryBreakdownItemDto(
                    CategoryId: g.Key,
                    CategoryName: categories.TryGetValue(g.Key, out var c) ? c.Name : string.Empty,
                    MonthlyAmount: monthly,
                    Percentage: pct,
                    SubscriptionCount: g.Count());
            })
            .OrderByDescending(i => i.MonthlyAmount)
            .ToList();

        // Rounding correction: ensure percentages sum to 100 by adjusting the last item.
        var pctSum = items.Sum(i => i.Percentage);
        if (items.Count > 0 && pctSum != 100m)
        {
            var delta = 100m - pctSum;
            var last = items[^1];
            items[^1] = last with { Percentage = Math.Round(last.Percentage + delta, 1) };
        }

        return items;
    }

    public async Task<IReadOnlyList<MonthlyTrendItemDto>> GetTrendAsync(
        int months,
        CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        if (months <= 0)
        {
            months = _defaultTrendMonths;
        }

        if (months > _maxTrendMonths)
        {
            months = _maxTrendMonths;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddMonths(-(months - 1));
        var fromMonthStart = new DateOnly(from.Year, from.Month, 1);

        var payments = await uow.Payments.GetByUserInRangeAsync(userId, fromMonthStart, today, ct);

        var grouped = payments
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Sum(p => p.Amount));

        var result = new List<MonthlyTrendItemDto>(months);
        var cursor = fromMonthStart;
        while (cursor <= today)
        {
            grouped.TryGetValue((cursor.Year, cursor.Month), out var amount);
            result.Add(new MonthlyTrendItemDto(
                Year: cursor.Year,
                Month: cursor.Month,
                Amount: Math.Round(amount, 2),
                Currency: "TRY"));
            cursor = cursor.AddMonths(1);
        }

        return result;
    }

    public async Task<IReadOnlyList<UnusedSubscriptionDto>> GetUnusedAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new EntityNotFoundException("User", userId);

        var unused = await uow.Subscriptions.GetUnusedAsync(userId, user.ThresholdDays, ct);
        var categories = (await uow.Categories.ListAsync(ct)).ToDictionary(c => c.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return unused
            .Select(s => s.ToUnusedDto(
                categories.TryGetValue(s.CategoryId, out var c) ? c.Name : string.Empty,
                today))
            .ToList();
    }

    public async Task<IReadOnlyList<InsightDto>> GetInsightsAsync(CancellationToken ct = default)
    {
        var (user, active) = await LoadUserAndActiveAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = (await uow.Categories.ListAsync(ct)).ToDictionary(c => c.Id);
        var insights = new List<Insight>();

        // 1) Unused subscriptions
        foreach (var s in active.Where(s => IsUnused(s, user.ThresholdDays, today)))
        {
            var monthly = Math.Round(BillingMath.ToMonthlyAmount(s.Amount, s.BillingCycle), 2);
            insights.Add(new Insight(
                InsightType.UnusedSubscription,
                $"{s.Name} kullanilmiyor",
                $"Son {user.ThresholdDays} gun icinde kullanim yok. Aylik {monthly} {s.Currency} tasarruf edebilirsiniz.",
                InsightSeverity.Warning,
                RelatedSubscriptionId: s.Id,
                PotentialSavings: monthly));
        }

        // 2) High monthly spending
        var monthlyTotal = active.Sum(s => BillingMath.ToMonthlyAmount(s.Amount, s.BillingCycle));
        if (monthlyTotal > _highSpendingThresholdTry)
        {
            insights.Add(new Insight(
                InsightType.HighSpending,
                "Aylik harcama yuksek",
                $"Toplam aylik abonelik harcamaniz {Math.Round(monthlyTotal, 2)} {user.PreferredCurrency}. Bu, esikten ({_highSpendingThresholdTry} {user.PreferredCurrency}) yuksek.",
                InsightSeverity.Critical));
        }

        // 3) Upcoming renewals
        foreach (var s in active.Where(s => s.NextBilling <= today.AddDays(_upcomingDays) && s.NextBilling >= today))
        {
            insights.Add(new Insight(
                InsightType.UpcomingRenewal,
                $"{s.Name} yakinda yenileniyor",
                $"{s.NextBilling:yyyy-MM-dd} tarihinde {s.Amount} {s.Currency} tahsil edilecek.",
                InsightSeverity.Info,
                RelatedSubscriptionId: s.Id));
        }

        // 4) Duplicate services (same category >= threshold subs)
        foreach (var group in active.GroupBy(s => s.CategoryId).Where(g => g.Count() >= _duplicateCategoryThreshold))
        {
            var categoryName = categories.TryGetValue(group.Key, out var c) ? c.Name : "Bilinmeyen";
            insights.Add(new Insight(
                InsightType.DuplicateService,
                $"{categoryName} kategorisinde {group.Count()} abonelik var",
                $"Birden fazla {categoryName} aboneliginiz oldugu icin birini iptal etmeyi dusunebilirsiniz.",
                InsightSeverity.Info));
        }

        return insights.Select(i => i.ToDto()).ToList();
    }

    private async Task<(User user, List<Subscription> active)> LoadUserAndActiveAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new EntityNotFoundException("User", userId);
        var subs = await uow.Subscriptions.GetByUserAsync(userId, ct);
        var active = subs.Where(s => s.Status == SubscriptionStatus.Active).ToList();
        return (user, active);
    }

    private static bool IsUnused(Subscription s, int thresholdDays, DateOnly today)
    {
        if (s.LastUsedDate is null)
        {
            return true;
        }

        var threshold = today.AddDays(-thresholdDays);
        return s.LastUsedDate < threshold;
    }
}
