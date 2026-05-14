namespace SubTrack.Api.Services;

/// <summary>Background sweep removing expired blacklist entries every minute.</summary>
public sealed class TokenBlacklistCleanupService(
    ITokenBlacklist blacklist,
    ILogger<TokenBlacklistCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token blacklist cleanup running every {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                var removed = blacklist.CleanupExpired();
                if (removed > 0)
                {
                    logger.LogDebug("Removed {Count} expired blacklist entries", removed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during blacklist cleanup");
            }
        }
    }
}
