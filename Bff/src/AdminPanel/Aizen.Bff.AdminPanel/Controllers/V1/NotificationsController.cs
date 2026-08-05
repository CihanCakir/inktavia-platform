using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
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

    // ─── Web push (N-A) ─────────────────────────────────────────────────────────
    // Mirrors the MarineProvider BFF's push proxy. The module resolves the acting admin from the
    // forwarded user assertion, so no identity is threaded here — just forward + unwrap the envelope.

    /// <summary>GET /api/v1/admin-panel/notifications/vapid-public-key — VAPID public key for pushManager.subscribe</summary>
    [HttpGet("vapid-public-key")]
    [ProducesResponseType(typeof(AizenApiResponse<VapidPublicKeyResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VapidPublicKeyResponse>> GetVapidPublicKey(CancellationToken ct)
    {
        var result = await _remote.GetVapidPublicKeyAsync(ct);
        return SetResponse(result.Body);
    }

    /// <summary>POST /api/v1/admin-panel/notifications/push-subscriptions — register the browser's WebPush subscription</summary>
    [HttpPost("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse>> Subscribe(
        [FromBody] PushSubscriptionRequest body,
        CancellationToken ct)
    {
        var result = await _remote.RegisterWebPushSubscriptionAsync(body, ct);
        return SetResponse(result.Body);
    }

    /// <summary>DELETE /api/v1/admin-panel/notifications/push-subscriptions — deactivate a WebPush subscription</summary>
    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse>> Unsubscribe(
        [FromBody] PushUnsubscribeRequest body,
        CancellationToken ct)
    {
        var result = await _remote.DeactivateWebPushSubscriptionAsync(body, ct);
        return SetResponse(result.Body);
    }

    // ─── N-B notification preferences ────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/notifications/preferences — the category×channel matrix for the admin</summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse>> GetPreferences(CancellationToken ct)
    {
        var result = await _remote.GetPreferencesAsync(ct);
        return SetResponse(result.Body);
    }

    /// <summary>PUT /api/v1/admin-panel/notifications/preferences — toggle one category×channel cell</summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse>> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceRequest body,
        CancellationToken ct)
    {
        var result = await _remote.UpdatePreferenceAsync(body, ct);
        return SetResponse(result.Body);
    }
}
