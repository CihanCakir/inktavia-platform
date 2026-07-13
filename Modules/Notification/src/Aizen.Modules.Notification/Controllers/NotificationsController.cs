using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;
using Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;
using Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;
using Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;
using Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;
using Aizen.Modules.Notification.Application.Query.GetUserNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

[ApiController]
[Route("api/v1/notification/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender            _sender;
    private readonly IAizenInfoAccessor _info;

    public NotificationsController(ISender sender, IAizenInfoAccessor info)
    {
        _sender = sender;
        _info   = info;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var result = await _sender.Send(
            new GetUserNotificationsQuery { UserId = userId, Skip = skip, Take = take }, ct);
        return Ok(result);
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        var ok = await _sender.Send(
            new MarkNotificationAsReadCommand { NotificationId = id, RequestingUserId = userId }, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        await _sender.Send(new BulkMarkAsReadCommand { UserId = userId }, ct);
        return NoContent();
    }

    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenRequest body,
        CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        await _sender.Send(new RegisterDeviceTokenCommand
        {
            UserId      = userId,
            DeviceToken = body.DeviceToken,
            Platform    = body.Platform,
        }, ct);
        return NoContent();
    }
    [HttpPost("push-subscriptions")]
    public async Task<IActionResult> RegisterWebPushSubscription(
        [FromBody] RegisterWebPushSubscriptionRequest body,
        CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        if (userId <= 0)
            return Unauthorized();

        await _sender.Send(new RegisterWebPushSubscriptionCommand
        {
            UserId   = userId,
            Endpoint = body.Endpoint,
            P256dh   = body.P256dh,
            Auth     = body.Auth,
        }, ct);
        return NoContent();
    }

    [HttpDelete("push-subscriptions")]
    public async Task<IActionResult> DeactivateWebPushSubscription(
        [FromBody] DeactivateWebPushSubscriptionRequest body,
        CancellationToken ct)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;
        if (userId <= 0)
            return Unauthorized();

        await _sender.Send(new DeactivateWebPushSubscriptionCommand
        {
            Endpoint = body.Endpoint,
        }, ct);
        return NoContent();
    }
}

public sealed class RegisterWebPushSubscriptionRequest
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public sealed class DeactivateWebPushSubscriptionRequest
{
    public string Endpoint { get; set; } = default!;
}

public sealed class RegisterDeviceTokenRequest
{
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
