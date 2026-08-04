using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
// Under /admin-panel like every other admin BFF controller — the admin-web httpClient baseURL is
// /api/v1/admin-panel, so a bare /api/v1/notifications route is unreachable (the FE's calls 404'd, which is
// why the notification inbox + badge were empty). Aligns with AdminMessagingController's /admin-panel/messaging.
[Route("api/v1/admin-panel/notifications")]
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
    [ProducesResponseType(typeof(AizenApiResponse<NotificationListBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationListBffDto>> GetMyNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _remote.GetMyNotificationsAsync(skip, take, ct);
        return SetResponse(result.Body);
    }

    /// <summary>
    /// GET /api/v1/notifications/unread-count — the badge count for the authenticated admin. The Notification module
    /// has no dedicated count endpoint; the unread count rides on the list response (Redis-cached per recipient), so
    /// fetch a minimal page and surface just the count. Shape matches the FE <c>NotificationUnreadCountDto</c>.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationUnreadCountBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationUnreadCountBffDto>> GetUnreadCount(CancellationToken ct)
    {
        var result = await _remote.GetMyNotificationsAsync(0, 1, ct);
        return SetResponse(new NotificationUnreadCountBffDto { UnreadCount = result.Body?.UnreadCount ?? 0 });
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
