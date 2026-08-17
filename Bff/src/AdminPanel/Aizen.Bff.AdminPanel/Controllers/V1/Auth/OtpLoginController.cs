using Aizen.Bff.AdminPanel.Application.Auth.Command;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1.Auth;

/// <summary>
/// BE Admin OTP → Keycloak login — a mirror of the MarineProvider BFF <c>OtpLoginController</c>. Verifies the OTP via the
/// Identity module (which mints the Keycloak token with the "Admin" realm role) and returns a LoginTicket for the FE handoff.
/// Replaces the legacy Identity HS256 login for the admin panel.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/auth/otp-login")]
[Tags("Admin Panel - OTP Login")]
[AllowAnonymous]
public sealed class OtpLoginController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public OtpLoginController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpPost("request")]
    [ProducesResponseType(typeof(OtpLoginRequestResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginRequestResponse>> Request(
        [FromBody] OtpLoginRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestOtpLoginBffCommand { Channel = request.Channel, Identifier = request.Identifier }, ct);
        return SetResponse(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(OtpLoginVerifyResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginVerifyResponse>> Verify(
        [FromBody] OtpLoginVerifyRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new VerifyOtpLoginBffCommand { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode }, ct);
        return SetResponse(result);
    }

    [HttpPost("resend")]
    [ProducesResponseType(typeof(OtpLoginResendResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<OtpLoginResendResponse>> Resend(
        [FromBody] OtpLoginResendRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ResendOtpLoginBffCommand { LoginRequestId = request.LoginRequestId }, ct);
        return SetResponse(result);
    }
}
