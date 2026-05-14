namespace SubTrack.Api.Services;

public interface ITokenBlacklist
{
    Task AddAsync(string jti, DateTime expiresAt);
    Task<bool> IsBlacklistedAsync(string jti);

    /// <summary>Remove entries past their expiry; returns number removed.</summary>
    int CleanupExpired();

    /// <summary>Number of currently tracked entries — for tests/diagnostics.</summary>
    int Count { get; }
}
