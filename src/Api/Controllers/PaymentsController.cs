using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Api.Contracts.Payments;
using SubTrack.Api.Services;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/subscriptions/{subscriptionId:long}/payments")]
[Authorize]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Record(
        long subscriptionId,
        [FromBody] PaymentCreateRequest request,
        CancellationToken ct)
    {
        var dto = await paymentService.RecordPaymentAsync(subscriptionId, request, ct);
        return Created($"/api/subscriptions/{subscriptionId}/payments/{dto.Id}", dto);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> History(
        long subscriptionId,
        CancellationToken ct) =>
        Ok(await paymentService.GetHistoryAsync(subscriptionId, ct));
}
