using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Api.Contracts.Notifications;
using SubTrack.Api.Services;

namespace SubTrack.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(CancellationToken ct) =>
        Ok(await notificationService.GetByUserAsync(ct));

    [HttpGet("unread")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Unread(CancellationToken ct) =>
        Ok(await notificationService.GetUnreadAsync(ct));

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await notificationService.MarkReadAsync(id, ct);
        return NoContent();
    }
}
