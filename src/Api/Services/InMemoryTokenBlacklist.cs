using System.Collections.Concurrent;

namespace SubTrack.Api.Services;

/// <summary>
/// Process-lifetime JWT jti blacklist. Resets on server restart — acceptable
/// for the course project. Production would back this with Redis or the DB
/// (open item tracked in CLAUDE.md Bolum 16).
/// </summary>
public sealed class InMemoryTokenBlacklist : ITokenBlacklist
{
    private readonly ConcurrentDictionary<string, DateTime> _entries = new();

    public int Count => _entries.Count;

    public Task AddAsync(string jti, DateTime expiresAt)
    {
        _entries[jti] = expiresAt;
        return Task.CompletedTask;
    }

    public Task<bool> IsBlacklistedAsync(string jti) =>
        Task.FromResult(_entries.ContainsKey(jti));

    public int CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var removed = 0;

        foreach (var kv in _entries)
        {
            if (kv.Value < now && _entries.TryRemove(kv.Key, out _))
            {
                removed++;
            }
        }

        return removed;
    }
}
