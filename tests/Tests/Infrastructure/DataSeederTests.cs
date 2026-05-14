using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

public class DataSeederTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_Populates_Expected_Row_Counts()
    {
        var dbName = $"SeederTests_{Guid.NewGuid():N}";
        await using var db = CreateContext(dbName);

        await DataSeeder.SeedAsync(db);

        (await db.Categories.CountAsync()).Should().Be(5);
        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Subscriptions.CountAsync()).Should().Be(7);
        (await db.Payments.CountAsync()).Should().Be(3);
        (await db.Notifications.CountAsync()).Should().Be(2);
        (await db.UsageLogs.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent()
    {
        var dbName = $"SeederTests_{Guid.NewGuid():N}";

        await using (var db = CreateContext(dbName))
        {
            await DataSeeder.SeedAsync(db);
        }

        await using (var db = CreateContext(dbName))
        {
            await DataSeeder.SeedAsync(db);

            (await db.Categories.CountAsync()).Should().Be(5);
            (await db.Users.CountAsync()).Should().Be(1);
            (await db.Subscriptions.CountAsync()).Should().Be(7);
            (await db.Payments.CountAsync()).Should().Be(3);
            (await db.Notifications.CountAsync()).Should().Be(2);
            (await db.UsageLogs.CountAsync()).Should().Be(3);
        }
    }

    [Fact]
    public async Task SeedAsync_Creates_Demo_User_With_Bcrypt_Hashed_Password()
    {
        var dbName = $"SeederTests_{Guid.NewGuid():N}";
        await using var db = CreateContext(dbName);

        await DataSeeder.SeedAsync(db);

        var user = await db.Users.SingleAsync();
        user.Email.Should().Be("demo@subtrack.app");
        user.FirstName.Should().Be("Demo");
        user.LastName.Should().Be("User");
        user.ThresholdDays.Should().Be(30);

        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe("Test1234!", "password must be hashed, not stored in cleartext");
        BCrypt.Net.BCrypt.Verify("Test1234!", user.PasswordHash).Should().BeTrue();
    }
}
