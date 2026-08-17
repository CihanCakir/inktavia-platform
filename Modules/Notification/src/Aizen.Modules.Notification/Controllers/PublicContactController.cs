using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Command.SubmitContactMessage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Notification.Controllers;

/// <summary>
/// M4 — anonymous inbound contact intake for the public website. In-cluster only ([AllowAnonymous] behind the
/// NetworkPolicy; the stricter <c>contact-submit</c> rate limit lives on the BFF). The payload is untrusted: the
/// command validates shape, scores spam, persists, and (below threshold) notifies admins — always returning
/// <c>{ accepted, ticketRef }</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/notification/public")]
public sealed class PublicContactController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IHttpContextAccessor _http;

    public PublicContactController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _http = httpContextAccessor;
    }

    [HttpPost("contact")]
    [ProducesResponseType(typeof(AizenApiResponse<SubmitContactResponse>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubmitContactResponse?>> Contact(
        [FromBody] SubmitContactRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync<SubmitContactResponse>(new SubmitContactMessageCommand
        {
            Name         = body.Name,
            Email        = body.Email,
            Subject      = body.Subject,
            Message      = body.Message,
            SourcePage   = body.SourcePage,
            CaptchaToken = body.CaptchaToken,
            Honeypot     = body.Honeypot,
            ClientIp     = ResolveClientIp(),   // hashed (salted) inside the handler — never stored raw
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
