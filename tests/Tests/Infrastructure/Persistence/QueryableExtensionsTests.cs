using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubTrack.Domain.Entities;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Extensions;

namespace SubTrack.Tests.Infrastructure.Persistence;

public class QueryableExtensionsTests
{
    private static AppDbContext NewInMemoryContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public async Task ToPagedResultAsync_Computes_TotalCount_Pages_Correctly()
    {
        var dbName = $"QueryableTests_{Guid.NewGuid():N}";
        await using var ctx = NewInMemoryContext(dbName);
        for (var i = 1; i <= 25; i++)
        {
            ctx.Categories.Add(new Category
            {
                Name = $"Cat-{i:D2}",
                Icon = "tag",
                Color = "#000000",
                SortOrder = i
            });
        }
        await ctx.SaveChangesAsync();

        var page2 = await ctx.Categories
            .OrderBy(c => c.SortOrder)
            .ToPagedResultAsync(page: 2, pageSize: 10);

        page2.Items.Should().HaveCount(10);
        page2.TotalCount.Should().Be(25);
        page2.TotalPages.Should().Be(3);
        page2.Page.Should().Be(2);
        page2.HasPrevious.Should().BeTrue();
        page2.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task ToPagedResultAsync_Caps_PageSize_At_100()
    {
        var dbName = $"QueryableTests_{Guid.NewGuid():N}";
        await using var ctx = NewInMemoryContext(dbName);
        for (var i = 0; i < 5; i++)
        {
            ctx.Categories.Add(new Category
            {
                Name = $"X-{i}",
                Icon = "tag",
                Color = "#000",
                SortOrder = i
            });
        }
        await ctx.SaveChangesAsync();

        var result = await ctx.Categories.OrderBy(c => c.SortOrder).ToPagedResultAsync(page: 1, pageSize: 5000);

        result.PageSize.Should().Be(100); // capped
        result.Items.Should().HaveCount(5); // but only 5 rows exist
    }

    [Fact]
    public async Task ApplyOrdering_Uses_Whitelist_When_SortBy_Is_Disallowed()
    {
        var dbName = $"QueryableTests_{Guid.NewGuid():N}";
        await using var ctx = NewInMemoryContext(dbName);
        ctx.Categories.AddRange(
            new Category { Name = "Beta", Icon = "tag", Color = "#000", SortOrder = 2 },
            new Category { Name = "Alpha", Icon = "tag", Color = "#000", SortOrder = 1 });
        await ctx.SaveChangesAsync();

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" };
        var ordered = await ctx.Categories
            .ApplyOrdering(sortBy: "Name", allowedFields: allowed, fallback: c => c.SortOrder)
            .ToListAsync();

        ordered[0].Name.Should().Be("Alpha");

        // Disallowed field falls back to SortOrder ascending → Alpha first
        var fallback = await ctx.Categories
            .ApplyOrdering(sortBy: "Password; DROP TABLE", allowedFields: allowed, fallback: c => c.SortOrder)
            .ToListAsync();

        fallback[0].Name.Should().Be("Alpha"); // SortOrder 1
    }
}
