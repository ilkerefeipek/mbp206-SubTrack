using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class PaymentRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetBySubscriptionAsync_OrderedDescending_ByPaymentDate()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var netflix = await ctx.Subscriptions.SingleAsync(s => s.Name == "Netflix Premium");
        var repo = new PaymentRepository(ctx);

        var payments = await repo.GetBySubscriptionAsync(netflix.Id);

        payments.Should().HaveCount(2);
        payments[0].PaymentDate.Should().BeOnOrAfter(payments[1].PaymentDate);
    }

    [Fact]
    public async Task GetByUserInRangeAsync_Filters_By_Date_Range()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new PaymentRepository(ctx);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddMonths(-1).AddDays(-3);
        var to = today;

        var payments = await repo.GetByUserInRangeAsync(demoUser.Id, from, to);

        payments.Should().OnlyContain(p => p.PaymentDate >= from && p.PaymentDate <= to);
    }

    [Fact]
    public async Task GetTotalAmountAsync_Sums_Correctly()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new PaymentRepository(ctx);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddYears(-1);
        var to = today;

        var total = await repo.GetTotalAmountAsync(demoUser.Id, from, to);

        // Seed: 229.99 x 2 (Netflix) + 59.99 (Spotify) = 519.97
        total.Should().Be(519.97m);
    }

    [Fact]
    public async Task GetTotalAmountAsync_Returns_Zero_For_EmptyRange()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var demoUser = await ctx.Users.SingleAsync();
        var repo = new PaymentRepository(ctx);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var total = await repo.GetTotalAmountAsync(demoUser.Id, future, future.AddMonths(1));

        total.Should().Be(0m);
    }

    [Fact]
    public async Task AddAsync_OrphanSubscriptionId_FK_Throws()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new PaymentRepository(ctx);

        await repo.AddAsync(new Payment
        {
            SubscriptionId = 999999,
            Amount = 10m,
            Currency = "TRY",
            Method = "credit_card",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = PaymentStatus.Success
        });

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
