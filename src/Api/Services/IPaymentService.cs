using SubTrack.Api.Contracts.Payments;

namespace SubTrack.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> RecordPaymentAsync(
        long subscriptionId,
        PaymentCreateRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<PaymentDto>> GetHistoryAsync(
        long subscriptionId,
        CancellationToken ct = default);
}
