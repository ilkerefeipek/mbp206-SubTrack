using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Common;

[Collection("Database")]
public abstract class RepositoryTestBase
{
    protected readonly DatabaseFixture Fixture;

    protected RepositoryTestBase(DatabaseFixture fixture) => Fixture = fixture;

    /// <summary>Reset the test DB and return a fresh DbContext. Caller disposes.</summary>
    protected async Task<AppDbContext> ArrangeAsync(bool seed = false)
    {
        await Fixture.ResetAsync(seed);
        return Fixture.CreateContext();
    }

    /// <summary>Convenience: insert a User with optional overrides.</summary>
    protected static User MakeUser(
        string email = "test@example.com",
        string first = "Test",
        string last = "User",
        int thresholdDays = 30) =>
        new()
        {
            Email = email,
            PasswordHash = "$2a$10$placeholderHashForTestsOnly..............",
            FirstName = first,
            LastName = last,
            ThresholdDays = thresholdDays,
            PreferredCurrency = "TRY",
            IsVerified = true
        };

    protected static Category MakeCategory(string name = "Test", bool isDefault = false) =>
        new() { Name = name, Icon = "tag", Color = "#000000", IsDefault = isDefault, SortOrder = 0 };

    protected static Subscription MakeSubscription(
        long userId,
        long categoryId,
        string name = "Test Sub",
        decimal amount = 50m,
        SubscriptionStatus status = SubscriptionStatus.Active,
        DateOnly? lastUsed = null,
        DateOnly? nextBilling = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new Subscription
        {
            UserId = userId,
            CategoryId = categoryId,
            Name = name,
            Amount = amount,
            Currency = "TRY",
            BillingCycle = BillingCycle.Monthly,
            NextBilling = nextBilling ?? today.AddDays(15),
            LastUsedDate = lastUsed,
            Status = status
        };
    }
}
