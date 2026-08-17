using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestProviderOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyProviderOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendProviderOtpLogin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

[ApiController]
[Route("api/v1/identity/auth/provider-otp-login")]
[Tags("Identity - Provider OTP Login")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ProviderOtpLoginController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ProviderOtpLoginController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor) { _sender = cqrsProcessor; }

    [HttpPost("request")]
    [ProducesResponseType(typeof(RequestProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestProviderOtpLoginResponse>> Request(
        [FromBody] RequestProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new RequestProviderOtpLoginCommand { Channel = request.Channel, Identifier = request.Identifier };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> Verify(
        [FromBody] VerifyProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new VerifyProviderOtpLoginCommand { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderOtpLoginResponse>> Resend(
        [FromBody] ResendProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new ResendProviderOtpLoginCommand { LoginRequestId = request.LoginRequestId };
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
}
