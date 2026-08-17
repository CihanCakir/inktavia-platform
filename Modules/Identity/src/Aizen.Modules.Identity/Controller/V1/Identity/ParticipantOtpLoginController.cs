using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestParticipantOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyParticipantOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendParticipantOtpLogin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

[ApiController]
[Route("api/v1/identity/auth/participant-otp-login")]
[Tags("Identity - Participant OTP Login")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ParticipantOtpLoginController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ParticipantOtpLoginController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor) { _sender = cqrsProcessor; }

    [HttpPost("request")]
    [ProducesResponseType(typeof(RequestProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestProviderOtpLoginResponse>> Request(
        [FromBody] RequestProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new RequestParticipantOtpLoginCommand { Channel = request.Channel, Identifier = request.Identifier };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> Verify(
        [FromBody] VerifyProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new VerifyParticipantOtpLoginCommand { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderOtpLoginResponse>> Resend(
        [FromBody] ResendProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new ResendParticipantOtpLoginCommand { LoginRequestId = request.LoginRequestId };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Server-to-server only (Keycloak SPI -> Identity). Consumes a login ticket jti atomically.</summary>
    [HttpPost("consume-ticket")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> ConsumeTicket(
        [FromBody] ConsumeTicketRequest request,
        [FromHeader(Name = "X-Otp-Login-Consume-Secret")] string? consumeSecret,
        CancellationToken ct)
    {
        var expectedSecret = HttpContext.RequestServices
            .GetRequiredService<IOptions<OtpLoginTicketOptions>>().Value.ConsumeSecret;
        if (string.IsNullOrEmpty(consumeSecret) || !string.Equals(consumeSecret, expectedSecret, StringComparison.Ordinal))
            return Unauthorized();

        var ticketService = HttpContext.RequestServices.GetRequiredService<IProviderOtpLoginTicketService>();
        var result = await ticketService.ConsumeAsync(request.Jti, ct);
        if (!result.Consumed)
            return StatusCode(StatusCodes.Status410Gone, new { consumed = false });

        return Ok(new { consumed = true, sub = result.Sub });
    }

    /// <summary>
    /// Server-to-server only (mobile BFF -> Identity, IdentityWrite). Mints a single-use login_ticket for an
    /// already-authenticated participant subject — used by password/social sign-in, which have no OTP step, so
    /// the BFF can run the SAME ParticipantSessionHandoff (auth-code+PKCE against inktavia-mobile) as OTP does.
    /// </summary>
    [HttpPost("mint-ticket")]
    [ProducesResponseType(typeof(MintParticipantTicketResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MintParticipantTicketResponse>> MintTicket(
        [FromBody] MintParticipantTicketRequest request, CancellationToken ct)
    {
        var ticketService = HttpContext.RequestServices.GetRequiredService<IProviderOtpLoginTicketService>();
        var ticket = await ticketService.MintAsync(request.Sub, "inktavia-mobile", ct);
        return SetResponse(new MintParticipantTicketResponse
        {
            LoginTicket = ticket.LoginTicket,
            ExpiresInSeconds = ticket.ExpiresInSeconds,
        });
    }

    /// <summary>
    /// Server-to-server only (mobile BFF -> Identity, IdentityWrite). Resolves a login identifier (email|phone)
    /// to the participant's canonical email (= Keycloak username) + subject, WITHOUT sending an OTP — reuses the
    /// SAME lookup + participant gate as OTP-login. Phone password-login uses this so it can run the Keycloak
    /// ROPC (whose username is the email) even though the phone is not stored in Keycloak. Found=false when
    /// no active participant matches (caller treats it as invalid credentials).
    /// </summary>
    [HttpPost("resolve-identifier")]
    [ProducesResponseType(typeof(ResolveParticipantIdentifierResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResolveParticipantIdentifierResponse>> ResolveIdentifier(
        [FromBody] ResolveParticipantIdentifierRequest request, CancellationToken ct)
    {
        var service = HttpContext.RequestServices.GetRequiredService<IParticipantOtpLoginDomainService>();
        var resolution = await service.ResolveByIdentifierAsync(request.Channel, request.Identifier, ct);
        return SetResponse(new ResolveParticipantIdentifierResponse
        {
            Found = resolution is not null,
            Email = resolution?.Email,
            KeycloakSubjectId = resolution?.KeycloakSubjectId,
        });
    }
}
