using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public Task<IActionResult> GetMine(
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _notificationService.GetForUserAsync(
            userId,
            isRead,
            cancellationToken));
    }

    [HttpGet("unread-count")]
    public Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _notificationService.GetUnreadCountAsync(
            userId,
            cancellationToken));
    }

    [HttpPatch("{notificationId:int}/read")]
    public Task<IActionResult> MarkAsRead(
        int notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _notificationService.MarkAsReadAsync(
            userId,
            notificationId,
            cancellationToken));
    }

    [HttpPatch("read-all")]
    public Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken));
    }

    private bool TryGetUserId(out int userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }

    private Task<IActionResult> InvalidSubject()
        => Task.FromResult<IActionResult>(Error(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "The access token is missing a valid subject claim."));
}
