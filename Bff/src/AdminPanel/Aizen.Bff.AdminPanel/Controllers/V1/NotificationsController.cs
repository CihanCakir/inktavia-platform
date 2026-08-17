using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
[Authorize]
public sealed class NotificationsController : AizenWebApiController
{
    private readonly INotificationBffRemoteCall _remote;

    public NotificationsController(
        IHttpContextAccessor httpContextAccessor,
        INotificationBffRemoteCall remote)
        : base(httpContextAccessor)
    {
        _remote = remote;
    }

    /// <summary>GET /api/v1/notifications — paginated inbox for the authenticated user</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationListBffDto>> GetMyNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _remote.GetMyNotificationsAsync(skip, take, ct);
        return SetResponse(result);
    }

    /// <summary>PATCH /api/v1/notifications/{id}/read — mark a single notification as read</summary>
    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _remote.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/notifications/mark-all-read — mark all notifications read for the authenticated user</summary>
    [HttpPost("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _remote.MarkAllReadAsync(ct);
        return NoContent();
    }
}
