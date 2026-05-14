namespace SubTrack.Client.Services.Toast;

public enum ToastVariant { Success, Error, Warning, Info }

public sealed record ToastMessage(Guid Id, string Message, ToastVariant Variant, int DurationMs);

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    event Action<Guid>? OnDismiss;

    void ShowSuccess(string message, int durationMs = 4000);
    void ShowError(string message, int durationMs = 5000);
    void ShowWarning(string message, int durationMs = 4500);
    void ShowInfo(string message, int durationMs = 4000);
    void Dismiss(Guid id);
}
