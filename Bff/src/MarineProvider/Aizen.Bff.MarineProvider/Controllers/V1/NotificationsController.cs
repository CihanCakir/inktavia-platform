using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Notifications;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
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

    [HttpPost("push-subscriptions")]
    [ProducesResponseType(typeof(SubscribePushResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribePushResponse?>> Subscribe(
        [FromBody] PushSubscriptionRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new SubscribePushCommand
        {
            Endpoint = body.Endpoint,
            P256dh = body.Keys.P256dh,
            Auth = body.Keys.Auth,
        }, ct));

    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(typeof(UnsubscribePushResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UnsubscribePushResponse?>> Unsubscribe(
        [FromBody] PushUnsubscribeRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync(new UnsubscribePushCommand
        {
            Endpoint = body.Endpoint,
        }, ct));
}
