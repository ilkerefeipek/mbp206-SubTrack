using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure;

[Collection("Database")]
public class UnitOfWorkTests(DatabaseFixture fixture)
{
    private UnitOfWork CreateUow(SubTrack.Infrastructure.Persistence.AppDbContext ctx) =>
        new(ctx,
            new UserRepository(ctx),
            new CategoryRepository(ctx),
            new SubscriptionRepository(ctx),
            new PaymentRepository(ctx),
            new NotificationRepository(ctx),
            new UsageLogRepository(ctx));

    [Fact]
    public async Task SaveChangesAsync_Persists_Repository_Changes()
    {
        await fixture.ResetAsync(seed: false);
        await using var ctx = fixture.CreateContext();
        var uow = CreateUow(ctx);

        var cat = new SubTrack.Domain.Entities.Category
        {
            Name = "Aliexpress",
            Icon = "shopping-bag",
            Color = "#FF6900",
            IsDefault = false,
            SortOrder = 99
        };
        await uow.Categories.AddAsync(cat);
        await uow.SaveChangesAsync();

        await using var verify = fixture.CreateContext();
        (await verify.Categories.AnyAsync(c => c.Name == "Aliexpress")).Should().BeTrue();
    }

    [Fact]
    public async Task BeginTransactionAsync_Rollback_Discards_All_Changes()
    {
        await fixture.ResetAsync(seed: false);
        await using var ctx = fixture.CreateContext();
        var uow = CreateUow(ctx);

        await using (var tx = await uow.BeginTransactionAsync())
        {
            await uow.Categories.AddAsync(new SubTrack.Domain.Entities.Category
            {
                Name = "Discarded",
                Icon = "x",
                Color = "#000",
                IsDefault = false,
                SortOrder = 1
            });
            await uow.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        await using var verify = fixture.CreateContext();
        (await verify.Categories.AnyAsync(c => c.Name == "Discarded")).Should().BeFalse();
    }

    [Fact]
    public async Task BeginTransactionAsync_Commit_Persists_Changes()
    {
        await fixture.ResetAsync(seed: false);
        await using var ctx = fixture.CreateContext();
        var uow = CreateUow(ctx);

        await using (var tx = await uow.BeginTransactionAsync())
        {
            await uow.Categories.AddAsync(new SubTrack.Domain.Entities.Category
            {
                Name = "Committed",
                Icon = "check",
                Color = "#0F766E",
                IsDefault = false,
                SortOrder = 1
            });
            await uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await using var verify = fixture.CreateContext();
        (await verify.Categories.AnyAsync(c => c.Name == "Committed")).Should().BeTrue();
    }

    [Fact]
    public async Task MultiRepository_SingleSaveChanges_Atomic()
    {
        await fixture.ResetAsync(seed: false);
        await using var ctx = fixture.CreateContext();
        var uow = CreateUow(ctx);

        var cat = new SubTrack.Domain.Entities.Category
        {
            Name = "Bundle",
            Icon = "package",
            Color = "#14B8A6",
            IsDefault = false,
            SortOrder = 1
        };
        await uow.Categories.AddAsync(cat);

        var user = RepositoryTestBase_ExposeMakeUser.Make("multi@subtrack.app");
        await uow.Users.AddAsync(user);

        await uow.SaveChangesAsync();

        await using var verify = fixture.CreateContext();
        (await verify.Categories.AnyAsync(c => c.Name == "Bundle")).Should().BeTrue();
        (await verify.Users.AnyAsync(u => u.Email == "multi@subtrack.app")).Should().BeTrue();
    }

    private static class RepositoryTestBase_ExposeMakeUser
    {
        public static SubTrack.Domain.Entities.User Make(string email) => new()
        {
            Email = email,
            PasswordHash = "$2a$10$placeholderHashForTestsOnly..............",
            FirstName = "Multi",
            LastName = "Repo",
            ThresholdDays = 30,
            PreferredCurrency = "TRY",
            IsVerified = true
        };
    }
}
