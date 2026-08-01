using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Identity.Service.OtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestAdminOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyAdminOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendAdminOtpLogin;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ProvisionAdminFromKeycloak;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

[ApiController]
[Route("api/v1/identity/auth/admin-otp-login")]
[Tags("Identity - Admin OTP Login")]
[Authorize(Policy = "IdentityWrite")]
public sealed class AdminOtpLoginController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public AdminOtpLoginController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor) { _sender = cqrsProcessor; }

    [HttpPost("request")]
    [ProducesResponseType(typeof(RequestProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestProviderOtpLoginResponse>> Request(
        [FromBody] RequestProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new RequestAdminOtpLoginCommand { Channel = request.Channel, Identifier = request.Identifier };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> Verify(
        [FromBody] VerifyProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new VerifyAdminOtpLoginCommand { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode };
        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(ResendProviderOtpLoginResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderOtpLoginResponse>> Resend(
        [FromBody] ResendProviderOtpLoginRequest request, CancellationToken ct)
    {
        var command = new ResendAdminOtpLoginCommand { LoginRequestId = request.LoginRequestId };
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
    /// Server-to-server only (Keycloak init / provisioning -> Identity). Links the Identity admin to the real
    /// Keycloak subject id, superseding the Kickoff-1 seed placeholder. Idempotent (find-by-sub -> find-by-email
    /// -> link -> create). Guarded by the shared consume-secret header, mirroring consume-ticket.
    /// </summary>
    [HttpPost("admin-provision")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminProvisionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminProvision(
        [FromBody] AdminProvisionRequest request,
        [FromHeader(Name = "X-Otp-Login-Consume-Secret")] string? consumeSecret,
        CancellationToken ct)
    {
        var expectedSecret = HttpContext.RequestServices
            .GetRequiredService<IOptions<OtpLoginTicketOptions>>().Value.ConsumeSecret;
        if (string.IsNullOrEmpty(consumeSecret) || !string.Equals(consumeSecret, expectedSecret, StringComparison.Ordinal))
            return Unauthorized();

        var command = new ProvisionAdminFromKeycloakCommand
        {
            KeycloakSubjectId = request.KeycloakSubjectId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailVerified = request.EmailVerified
        };
        var result = await _sender.ProcessAsync(command, ct);
        return Ok(result);
    }
}
