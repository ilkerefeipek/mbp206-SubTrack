using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Common;

/// <summary>
/// Provides a real LocalDB-backed AppDbContext for repository integration tests.
/// Lives in its own database (SubTrack_Test) so it never touches the dev DB.
/// Tests share this fixture via the Database xUnit collection — serial execution.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    public const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=SubTrack_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    /// <summary>Truncate every table in FK-safe order; optionally re-run the seed.</summary>
    public async Task ResetAsync(bool seed = false)
    {
        await using var ctx = CreateContext();

        await ctx.Database.ExecuteSqlRawAsync(@"
            DELETE FROM UsageLogs;
            DELETE FROM Notifications;
            DELETE FROM Payments;
            DELETE FROM Subscriptions;
            DELETE FROM Categories;
            DELETE FROM Users;
        ");

        if (seed)
        {
            await DataSeeder.SeedAsync(ctx);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
