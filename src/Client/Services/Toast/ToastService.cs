namespace SubTrack.Client.Services.Toast;

public sealed class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;
    public event Action<Guid>? OnDismiss;

    public void ShowSuccess(string message, int durationMs = 4000) =>
        Push(new ToastMessage(Guid.NewGuid(), message, ToastVariant.Success, durationMs));

    public void ShowError(string message, int durationMs = 5000) =>
        Push(new ToastMessage(Guid.NewGuid(), message, ToastVariant.Error, durationMs));

    public void ShowWarning(string message, int durationMs = 4500) =>
        Push(new ToastMessage(Guid.NewGuid(), message, ToastVariant.Warning, durationMs));

    public void ShowInfo(string message, int durationMs = 4000) =>
        Push(new ToastMessage(Guid.NewGuid(), message, ToastVariant.Info, durationMs));

    public void Dismiss(Guid id) => OnDismiss?.Invoke(id);

    private void Push(ToastMessage toast) => OnShow?.Invoke(toast);
}
