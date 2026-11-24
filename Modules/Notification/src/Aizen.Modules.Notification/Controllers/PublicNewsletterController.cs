using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.ConfirmNewsletterSubscription;
using Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;
using Aizen.Modules.Notification.Application.Command.UnsubscribeNewsletter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

/// <summary>
/// P1 — anonymous newsletter intake for the public website. In-cluster only ([AllowAnonymous] behind the NetworkPolicy;
/// the stricter <c>newsletter-submit</c> rate limit lives on the BFF). Double opt-in: subscribe only creates a Pending
/// row + confirm mail; the address becomes a subscriber only after /confirm redeems the token. Enumeration-safe.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/notification/public/newsletter")]
public sealed class PublicNewsletterController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IHttpContextAccessor _http;

    public PublicNewsletterController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _http = httpContextAccessor;
    }

    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterSubscribeResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterSubscribeResponse?>> Subscribe(
        [FromBody] SubscribeNewsletterRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<NewsletterSubscribeResponse>(new SubscribeNewsletterCommand
        {
            Email      = body.Email,
            Lang       = body.Lang,
            SourcePage = body.SourcePage,
            ClientIp   = ResolveClientIp(),   // hashed (salted) inside the handler — never stored raw
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterConfirmResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterConfirmResponse?>> Confirm(
        [FromBody] ConfirmNewsletterRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterConfirmResponse>(
               new ConfirmNewsletterSubscriptionCommand { Token = body.Token }, ct));

    [HttpPost("unsubscribe")]
    [ProducesResponseType(typeof(AizenApiResponse<NewsletterUnsubscribeResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NewsletterUnsubscribeResponse?>> Unsubscribe(
        [FromBody] UnsubscribeNewsletterRequest body, CancellationToken ct)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterUnsubscribeResponse>(
               new UnsubscribeNewsletterCommand { Token = body.Token }, ct));

    private string? ResolveClientIp()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}
