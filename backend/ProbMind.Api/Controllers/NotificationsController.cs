using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public Task<IReadOnlyList<NotificationDto>> List([FromQuery] bool unreadOnly = false, CancellationToken ct = default) =>
        _notifications.ListAsync(UserContext.UserId(User), unreadOnly, ct);

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(UserContext.UserId(User), notificationId, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(UserContext.UserId(User), ct);
        return NoContent();
    }
}
