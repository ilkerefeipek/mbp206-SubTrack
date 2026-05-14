using SubTrack.Client.Models;

namespace SubTrack.Client.Services.Api;

public interface IPaymentsApi
{
    Task<PaymentDto> RecordPaymentAsync(long subscriptionId, PaymentCreateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> GetHistoryAsync(long subscriptionId, CancellationToken ct = default);
}

public sealed class PaymentsApi(HttpClient http) : ApiClientBase(http), IPaymentsApi
{
    public Task<PaymentDto> RecordPaymentAsync(
        long subscriptionId,
        PaymentCreateRequest request,
        CancellationToken ct = default) =>
        PostAsync<PaymentDto>($"/api/subscriptions/{subscriptionId}/payments", request, ct);

    public async Task<IReadOnlyList<PaymentDto>> GetHistoryAsync(
        long subscriptionId,
        CancellationToken ct = default) =>
        await GetAsync<List<PaymentDto>>($"/api/subscriptions/{subscriptionId}/payments", ct);
}
