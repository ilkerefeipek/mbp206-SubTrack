using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubTrack.Api.Contracts.Payments;
using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Tests.Infrastructure;

namespace SubTrack.Tests.Api;

public class PaymentsApiTests : IClassFixture<SubTrackWebAppFactory>
{
    private readonly SubTrackWebAppFactory _factory;
    public PaymentsApiTests(SubTrackWebAppFactory factory) => _factory = factory;

    private async Task<(HttpClient client, SubscriptionDto sub)> SetupWithSubscriptionAsync()
    {
        await _factory.EnsureCategoriesSeededAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        }

        var client = _factory.CreateClientWithPartition();
        var auth = await client.RegisterFreshAsync();
        client.WithBearer(auth.Token);

        using var s = _factory.Services.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.Categories.OrderBy(c => c.SortOrder).Select(c => c.Id).FirstAsync();

        var createResp = await client.PostAsJsonAsync("/api/subscriptions", new
        {
            Name = "PayTest",
            CategoryId = categoryId,
            Amount = 100m,
            BillingCycle = BillingCycle.Monthly,
            NextBilling = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)).ToString("yyyy-MM-dd")
        });
        createResp.EnsureSuccessStatusCode();
        var sub = await createResp.Content.ReadFromJsonAsync<SubscriptionDto>();
        return (client, sub!);
    }

    private static object NewPayment(decimal amount = 100m) => new
    {
        Amount = amount,
        Method = "credit_card",
        PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
        TransactionId = "TEST-001"
    };

    [Fact]
    public async Task RecordPayment_ValidRequest_Returns201()
    {
        var (client, sub) = await SetupWithSubscriptionAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/subscriptions/{sub.Id}/payments",
            NewPayment());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<PaymentDto>();
        dto!.Amount.Should().Be(100m);
        dto.Status.Should().Be(PaymentStatus.Success);
    }

    [Fact]
    public async Task RecordPayment_NegativeAmount_Returns400()
    {
        var (client, sub) = await SetupWithSubscriptionAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/subscriptions/{sub.Id}/payments",
            NewPayment(amount: -5m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordPayment_NotOwnedSubscription_Returns404()
    {
        var (client1, sub) = await SetupWithSubscriptionAsync();

        var client2 = _factory.CreateClientWithPartition();
        var auth2 = await client2.RegisterFreshAsync();
        client2.WithBearer(auth2.Token);

        var response = await client2.PostAsJsonAsync(
            $"/api/subscriptions/{sub.Id}/payments",
            NewPayment());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordPayment_AdvancesSubscription_NextBilling()
    {
        var (client, sub) = await SetupWithSubscriptionAsync();
        var before = sub.NextBilling;

        await client.PostAsJsonAsync($"/api/subscriptions/{sub.Id}/payments", NewPayment());

        var reloaded = await client.GetFromJsonAsync<SubscriptionDto>($"/api/subscriptions/{sub.Id}");
        // Monthly cycle → next billing advances by one month.
        reloaded!.NextBilling.Should().Be(before.AddMonths(1));
    }

    [Fact]
    public async Task GetHistory_OwnedSubscription_ReturnsOrderedDesc()
    {
        var (client, sub) = await SetupWithSubscriptionAsync();
        await client.PostAsJsonAsync($"/api/subscriptions/{sub.Id}/payments", new
        {
            Amount = 100m,
            Method = "card",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd")
        });
        await client.PostAsJsonAsync($"/api/subscriptions/{sub.Id}/payments", new
        {
            Amount = 100m,
            Method = "card",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        });

        var history = await client.GetFromJsonAsync<List<PaymentDto>>($"/api/subscriptions/{sub.Id}/payments");

        history!.Should().HaveCountGreaterThanOrEqualTo(2);
        history[0].PaymentDate.Should().BeOnOrAfter(history[^1].PaymentDate);
    }

    [Fact]
    public async Task GetHistory_NotOwnedSubscription_Returns404()
    {
        var (_, sub) = await SetupWithSubscriptionAsync();
        var attacker = _factory.CreateClientWithPartition();
        var auth = await attacker.RegisterFreshAsync();
        attacker.WithBearer(auth.Token);

        var response = await attacker.GetAsync($"/api/subscriptions/{sub.Id}/payments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
