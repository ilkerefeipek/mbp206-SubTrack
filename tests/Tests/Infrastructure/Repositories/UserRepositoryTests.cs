using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class UserRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        var user = await repo.GetByEmailAsync("demo@subtrack.app");

        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Demo");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistent_ReturnsNull()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        var user = await repo.GetByEmailAsync("ghost@subtrack.app");

        user.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        var user = await repo.GetByEmailAsync("DEMO@SubTrack.APP");

        user.Should().NotBeNull();
        user!.Email.Should().Be("demo@subtrack.app");
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrue_WhenSeeded()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        (await repo.EmailExistsAsync("demo@subtrack.app")).Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsFalse_WhenAbsent()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        (await repo.EmailExistsAsync("nobody@subtrack.app")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_PersistsAfterSaveChanges()
    {
        await using var ctx = await ArrangeAsync(seed: false);
        var repo = new UserRepository(ctx);

        await repo.AddAsync(MakeUser(email: "newcomer@subtrack.app"));
        await ctx.SaveChangesAsync();

        (await repo.EmailExistsAsync("newcomer@subtrack.app")).Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsOnSave()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new UserRepository(ctx);

        await repo.AddAsync(MakeUser(email: "demo@subtrack.app"));

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
