namespace SubTrack.Client.Services;

/// <summary>
/// Cancels the previous pending action and schedules a new one after the
/// specified delay. Useful for search inputs to coalesce rapid keystrokes.
/// </summary>
public sealed class Debouncer : IDisposable
{
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public async Task DebounceAsync(int milliseconds, Func<Task> action)
    {
        if (_disposed)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            await Task.Delay(milliseconds, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await action();
        }
        catch (TaskCanceledException)
        {
            // Expected when a newer keystroke superseded this one.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
