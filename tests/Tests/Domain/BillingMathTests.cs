using FluentAssertions;
using SubTrack.Domain.Common;
using SubTrack.Domain.Enums;

namespace SubTrack.Tests.Domain;

public class BillingMathTests
{
    [Theory]
    [InlineData(BillingCycle.Monthly, 100, 100)]
    [InlineData(BillingCycle.Quarterly, 300, 100)]
    [InlineData(BillingCycle.Yearly, 1200, 100)]
    public void ToMonthlyAmount_KnownInputs_ProducesExpected(BillingCycle cycle, decimal amount, decimal expected)
    {
        var result = BillingMath.ToMonthlyAmount(amount, cycle);
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Fact]
    public void ToMonthlyAmount_Weekly_Uses_52_Over_12()
    {
        // 12 TRY/week ≈ 52 TRY/month
        var result = BillingMath.ToMonthlyAmount(12m, BillingCycle.Weekly);
        result.Should().BeApproximately(52m, 0.05m);
    }

    [Fact]
    public void ToMonthlyAmount_ZeroAmount_ReturnsZero()
    {
        BillingMath.ToMonthlyAmount(0m, BillingCycle.Monthly).Should().Be(0m);
        BillingMath.ToMonthlyAmount(0m, BillingCycle.Yearly).Should().Be(0m);
    }

    [Fact]
    public void ToMonthlyAmount_UnknownCycle_Throws()
    {
        var act = () => BillingMath.ToMonthlyAmount(100m, (BillingCycle)999);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(BillingCycle.Weekly, "2026-01-01", "2026-01-08")]
    [InlineData(BillingCycle.Monthly, "2026-01-15", "2026-02-15")]
    [InlineData(BillingCycle.Quarterly, "2026-01-15", "2026-04-15")]
    [InlineData(BillingCycle.Yearly, "2026-05-14", "2027-05-14")]
    public void AdvanceNextBilling_KnownInputs_ProducesExpected(
        BillingCycle cycle,
        string startIso,
        string expectedIso)
    {
        var start = DateOnly.Parse(startIso);
        var expected = DateOnly.Parse(expectedIso);

        BillingMath.AdvanceNextBilling(start, cycle).Should().Be(expected);
    }

    [Fact]
    public void AdvanceNextBilling_UnknownCycle_Throws()
    {
        var act = () => BillingMath.AdvanceNextBilling(
            DateOnly.FromDateTime(DateTime.UtcNow),
            (BillingCycle)999);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
