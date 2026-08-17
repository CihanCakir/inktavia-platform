using Aizen.Bff.Marine.Web.Application.Contact.Command.SubmitWebContact;
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
/// Public contact submit (M4) — anonymous WRITE on the STRICTER <c>contact-submit</c> rate limit (tighter than
/// <c>public-read-ip</c>). Thin: forwards the untrusted payload to Notification via CQRS; the module owns validation,
/// spam scoring, persistence, and the admin notify. The caller IP is forwarded (X-Forwarded-For) for the module to
/// hash — never trusted from the body. Response is only <c>{ accepted, ticketRef }</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/contact")]
[Tags("Web - Contact")]
public sealed class ContactController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IHttpContextAccessor _http;

    public ContactController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _http = httpContextAccessor;
    }

    [HttpPost]
    [EnableRateLimiting("contact-submit")]
    public async Task<AizenApiResponse<SubmitContactResponse?>> Submit(
        [FromBody] SubmitContactRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SubmitContactResponse>(new SubmitWebContactCommand
        {
            Name         = body.Name,
            Email        = body.Email,
            Subject      = body.Subject,
            Message      = body.Message,
            SourcePage   = body.SourcePage,
            CaptchaToken = body.CaptchaToken,
            Honeypot     = body.Honeypot,
            ClientIp     = ResolveClientIp(),
        }, ct);
        return SetResponse(result);
    }

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
