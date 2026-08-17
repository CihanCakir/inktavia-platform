using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Notifications;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/notifications")]
[Tags("Provider - Notifications")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
public sealed class NotificationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public NotificationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("vapid-public-key")]
    [ProducesResponseType(typeof(AizenApiResponse<VapidPublicKeyResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VapidPublicKeyResponse?>> GetVapidPublicKey(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetVapidPublicKeyBffQuery(), ct));

    [HttpGet]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationListResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationListResponse?>> List(
        [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetProviderNotificationsBffQuery { Skip = skip, Take = take }, ct));

    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkAllNotificationsReadResponse?>> MarkAllRead(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new MarkAllNotificationsReadBffCommand(), ct));

    [HttpPatch("{id:long}/read")]
    [ProducesResponseType(typeof(AizenApiResponse<MarkNotificationReadResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MarkNotificationReadResponse?>> MarkRead(long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new MarkNotificationReadBffCommand { Id = id }, ct));

    [HttpPost("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Subscribe(
        [FromBody] PushSubscriptionRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubscribePushCommand
        {
            Endpoint = body.Endpoint,
            P256dh = body.Keys.P256dh,
            Auth = body.Keys.Auth,
        }, ct));

    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(typeof(AizenApiResponse<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PushSubscriptionResponse?>> Unsubscribe(
        [FromBody] PushUnsubscribeRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UnsubscribePushCommand
        {
            Endpoint = body.Endpoint,
        }, ct));

    // ─── N-B notification preferences ────────────────────────────────────────────

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse?>> GetPreferences(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new GetNotificationPreferencesBffQuery(), ct));

    [HttpPut("preferences")]
    [ProducesResponseType(typeof(AizenApiResponse<NotificationPreferencesResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NotificationPreferencesResponse?>> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UpdateNotificationPreferenceBffCommand
        {
            Category = body.Category,
            Channel  = body.Channel,
            Enabled  = body.Enabled,
        }, ct));
}
