using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Api.Contracts.Subscriptions;
using SubTrack.Api.Services;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public sealed class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionListItemDto>>> List(
        [FromQuery] SubscriptionFilters filters,
        CancellationToken ct) =>
        Ok(await subscriptionService.ListAsync(filters, ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SubscriptionDto>> GetById(long id, CancellationToken ct) =>
        Ok(await subscriptionService.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> Create(
        [FromBody] SubscriptionCreateRequest request,
        CancellationToken ct)
    {
        var dto = await subscriptionService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SubscriptionDto>> Update(
        long id,
        [FromBody] SubscriptionUpdateRequest request,
        CancellationToken ct) =>
        Ok(await subscriptionService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await subscriptionService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/mark-used")]
    public async Task<IActionResult> MarkAsUsed(long id, CancellationToken ct)
    {
        await subscriptionService.MarkAsUsedAsync(id, ct);
        return NoContent();
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionListItemDto>>> GetUpcoming(
        [FromQuery] int daysAhead = 7,
        CancellationToken ct = default) =>
        Ok(await subscriptionService.GetUpcomingAsync(daysAhead, ct));
}
