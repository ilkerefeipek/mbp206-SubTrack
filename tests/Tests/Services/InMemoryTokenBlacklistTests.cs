using FluentAssertions;
using SubTrack.Api.Services;

namespace SubTrack.Tests.Services;

public class InMemoryTokenBlacklistTests
{
    [Fact]
    public async Task Add_ThenCheck_ReturnsTrue()
    {
        var bl = new InMemoryTokenBlacklist();
        var jti = Guid.NewGuid().ToString();

        await bl.AddAsync(jti, DateTime.UtcNow.AddHours(1));

        (await bl.IsBlacklistedAsync(jti)).Should().BeTrue();
        bl.Count.Should().Be(1);
    }

    [Fact]
    public async Task UnknownJti_IsBlacklistedAsync_ReturnsFalse()
    {
        var bl = new InMemoryTokenBlacklist();

        (await bl.IsBlacklistedAsync("never-added")).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupExpired_RemovesPastEntriesOnly()
    {
        var bl = new InMemoryTokenBlacklist();
        var past = Guid.NewGuid().ToString();
        var future = Guid.NewGuid().ToString();

        await bl.AddAsync(past, DateTime.UtcNow.AddMinutes(-1));
        await bl.AddAsync(future, DateTime.UtcNow.AddMinutes(60));

        var removed = bl.CleanupExpired();

        removed.Should().Be(1);
        (await bl.IsBlacklistedAsync(past)).Should().BeFalse();
        (await bl.IsBlacklistedAsync(future)).Should().BeTrue();
    }

    [Fact]
    public void CleanupExpired_NoEntries_ReturnsZero()
    {
        var bl = new InMemoryTokenBlacklist();
        bl.CleanupExpired().Should().Be(0);
    }
}
