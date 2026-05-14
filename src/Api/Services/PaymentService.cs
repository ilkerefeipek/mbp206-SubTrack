using SubTrack.Api.Contracts.Payments;
using SubTrack.Api.Mappings;
using SubTrack.Domain.Common;
using SubTrack.Domain.Common.Exceptions;
using SubTrack.Domain.Entities;
using SubTrack.Domain.Enums;

namespace SubTrack.Api.Services;

public sealed class PaymentService(
    IUnitOfWork uow,
    ICurrentUserService currentUser) : IPaymentService
{
    public async Task<PaymentDto> RecordPaymentAsync(
        long subscriptionId,
        PaymentCreateRequest request,
        CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var subscription = await LoadOwnedSubscriptionAsync(subscriptionId, userId, ct);

        var payment = new Payment
        {
            SubscriptionId = subscription.Id,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? subscription.Currency
                : request.Currency.ToUpperInvariant(),
            Method = request.Method.Trim(),
            PaymentDate = request.PaymentDate,
            Status = PaymentStatus.Success,
            TransactionId = request.TransactionId
        };

        await uow.Payments.AddAsync(payment, ct);

        // Advance the subscription's NextBilling to keep the renewal cycle moving.
        subscription.NextBilling = BillingMath.AdvanceNextBilling(
            subscription.NextBilling,
            subscription.BillingCycle);
        uow.Subscriptions.Update(subscription);

        await uow.SaveChangesAsync(ct);
        return payment.ToDto();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetHistoryAsync(
        long subscriptionId,
        CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        _ = await LoadOwnedSubscriptionAsync(subscriptionId, userId, ct);

        var payments = await uow.Payments.GetBySubscriptionAsync(subscriptionId, ct);
        return payments.Select(p => p.ToDto()).ToList();
    }

    private async Task<Subscription> LoadOwnedSubscriptionAsync(
        long subscriptionId,
        long userId,
        CancellationToken ct)
    {
        var subscription = await uow.Subscriptions.GetByIdAsync(subscriptionId, ct);
        if (subscription is null || subscription.UserId != userId)
        {
            throw new EntityNotFoundException("Subscription", subscriptionId);
        }
        return subscription;
    }
}
