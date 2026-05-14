using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Infrastructure.Repositories;
using SubTrack.Tests.Common;

namespace SubTrack.Tests.Infrastructure.Repositories;

public class CategoryRepositoryTests(DatabaseFixture fixture) : RepositoryTestBase(fixture)
{
    [Fact]
    public async Task GetDefaultsAsync_Returns_5_DefaultCategories_Ordered()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new CategoryRepository(ctx);

        var defaults = await repo.GetDefaultsAsync();

        defaults.Should().HaveCount(5);
        defaults.Select(c => c.SortOrder).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsMatchingCategory()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new CategoryRepository(ctx);

        var cat = await repo.GetByNameAsync("Streaming");

        cat.Should().NotBeNull();
        cat!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsOnSave()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new CategoryRepository(ctx);

        await repo.AddAsync(MakeCategory(name: "Streaming"));

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Remove_CategoryWithActiveSubscriptions_FailsWithRestrict()
    {
        await using var ctx = await ArrangeAsync(seed: true);
        var repo = new CategoryRepository(ctx);

        // Streaming category is referenced by 3 seeded subscriptions; FK Restrict blocks delete.
        var streaming = (await repo.GetByNameAsync("Streaming"))!;
        repo.Remove(streaming);

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
