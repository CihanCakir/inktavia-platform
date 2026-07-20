using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;
using Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;
using Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;
using Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;
using Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;
using Aizen.Modules.Notification.Application.Query.GetUserNotifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/notifications")]
[Authorize]
public sealed class NotificationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

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
}
