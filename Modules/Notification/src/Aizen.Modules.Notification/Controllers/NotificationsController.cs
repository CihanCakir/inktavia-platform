using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;
using Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;
using Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;
using Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;
using Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;
using Aizen.Modules.Notification.Application.Command.UpdateNotificationPreference;
using Aizen.Modules.Notification.Application.Query.GetNotificationPreferences;
using Aizen.Modules.Notification.Application.Query.GetUserNotifications;
using Aizen.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/notifications")]
[Authorize]
public sealed class NotificationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IOptions<VapidOptions> _vapidOptions;

    public NotificationsController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrs,
        IOptions<VapidOptions> vapidOptions)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _vapidOptions = vapidOptions;
    }

    [HttpGet("vapid-public-key")]
    [ProducesResponseType(typeof(AizenApiResponse<VapidPublicKeyResponse>), StatusCodes.Status200OK)]
    public AizenApiResponse<VapidPublicKeyResponse?> GetVapidPublicKey()
        => SetResponse(new VapidPublicKeyResponse { PublicKey = _vapidOptions.Value.PublicKey });

    [HttpGet]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationListResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationListResponse?>> List(
        [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationListResponse>(
               new GetUserNotificationsQuery { Skip = skip, Take = take }, ct));

    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkNotificationReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkNotificationReadResponse?>> MarkRead(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<MarkNotificationReadResponse>(
               new MarkNotificationAsReadCommand { NotificationId = id }, ct));

    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkAllNotificationsReadResponse?>> MarkAllRead(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<MarkAllNotificationsReadResponse>(new BulkMarkAsReadCommand(), ct));

    [HttpPost("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Subscribe(
        [FromBody] PushSubscriptionRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<PushSubscriptionResponse>(
               new RegisterWebPushSubscriptionCommand { Endpoint = body.Endpoint, P256dh = body.Keys.P256dh, Auth = body.Keys.Auth }, ct));

    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Unsubscribe(
        [FromBody] PushUnsubscribeRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<PushSubscriptionResponse>(
               new DeactivateWebPushSubscriptionCommand { Endpoint = body.Endpoint }, ct));

    [HttpPost("device-token")]
    [ProducesResponseType(typeof(AizenApiResponse<DeviceTokenResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DeviceTokenResponse?>> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<DeviceTokenResponse>(
               new RegisterDeviceTokenCommand { DeviceToken = body.DeviceToken, Platform = body.Platform }, ct));

    // ─── N-B notification preferences ────────────────────────────────────────────

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse?>> GetPreferences(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationPreferencesResponse>(
               new GetNotificationPreferencesQuery(), ct));

    [HttpPut("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse?>> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NotificationPreferencesResponse>(
               new UpdateNotificationPreferenceCommand
               {
                   Category = body.Category,
                   Channel  = body.Channel,
                   Enabled  = body.Enabled,
               }, ct));
}
