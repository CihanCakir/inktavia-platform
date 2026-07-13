using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Notifications;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/notifications")]
[Tags("Provider - Notifications")]
[Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
public sealed class ProviderNotificationsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderNotificationsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost("push-subscriptions")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new SubscribePushCommand
        {
            Endpoint = body.Endpoint,
            P256dh = body.Keys.P256dh,
            Auth = body.Keys.Auth,
        }, ct);
        return Ok(result);
    }

    [HttpDelete("push-subscriptions")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new UnsubscribePushCommand
        {
            Endpoint = body.Endpoint,
        }, ct);
        return Ok(result);
    }
}

public sealed class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = default!;
    public PushSubscriptionKeys Keys { get; set; } = default!;
}

public sealed class PushSubscriptionKeys
{
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public sealed class PushUnsubscribeRequest
{
    public string Endpoint { get; set; } = default!;
}
