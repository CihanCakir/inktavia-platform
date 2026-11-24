using Aizen.Bff.Marine.Web.Application.Newsletter.Command.ConfirmWebNewsletter;
using Aizen.Bff.Marine.Web.Application.Newsletter.Command.SubscribeWebNewsletter;
using Aizen.Bff.Marine.Web.Application.Newsletter.Command.UnsubscribeWebNewsletter;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public newsletter (P1) — anonymous WRITEs on the STRICTER <c>newsletter-submit</c> rate limit (same shape as
/// <c>contact-submit</c>). Thin: forwards the untrusted payload/token to Notification via CQRS; the module owns
/// validation, idempotency, spam scoring, persistence, and the double opt-in mail. The caller IP is forwarded
/// (X-Forwarded-For) on subscribe for the module to hash — never trusted from the body. NO caching (these are writes).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/newsletter")]
[Tags("Web - Newsletter")]
public sealed class NewsletterController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IHttpContextAccessor _http;

    public NewsletterController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _http = httpContextAccessor;
    }

    [HttpPost("subscribe")]
    [EnableRateLimiting("newsletter-submit")]
    public async Task<AizenApiResponse<NewsletterSubscribeResponse?>> Subscribe(
        [FromBody] SubscribeNewsletterRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NewsletterSubscribeResponse>(new SubscribeWebNewsletterCommand
        {
            Email      = body.Email,
            Lang       = body.Lang,
            SourcePage = body.SourcePage,
            ClientIp   = ResolveClientIp(),
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("confirm")]
    [EnableRateLimiting("newsletter-submit")]
    public async Task<AizenApiResponse<NewsletterConfirmResponse?>> Confirm(
        [FromBody] ConfirmNewsletterRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterConfirmResponse>(
               new ConfirmWebNewsletterCommand { Token = body.Token }, ct));

    [HttpPost("unsubscribe")]
    [EnableRateLimiting("newsletter-submit")]
    public async Task<AizenApiResponse<NewsletterUnsubscribeResponse?>> Unsubscribe(
        [FromBody] UnsubscribeNewsletterRequest body, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<NewsletterUnsubscribeResponse>(
               new UnsubscribeWebNewsletterCommand { Token = body.Token }, ct));

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
