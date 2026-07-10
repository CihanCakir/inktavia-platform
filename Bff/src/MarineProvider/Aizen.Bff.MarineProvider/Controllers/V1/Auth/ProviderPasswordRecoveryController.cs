using Aizen.Bff.MarineProvider.Application.Auth.Password.ForgotProviderPassword;
using Aizen.Bff.MarineProvider.Application.Auth.Password.ResendProviderPasswordOtp;
using Aizen.Bff.MarineProvider.Application.Auth.Password.ResetProviderPassword;
using Aizen.Bff.MarineProvider.Application.Auth.Password.VerifyProviderPasswordOtp;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Auth;

/// <summary>
/// BFF-orchestrated provider password recovery (all endpoints public/anonymous). The BFF owns OTP generation and
/// storage and updates the provider's Keycloak password server-side. No login token is issued; OTP is for reset
/// only. Thin controller — all logic lives in the CQRS handlers.
/// </summary>
[ApiController]
[Route("api/v1/provider/auth/password")]
[Tags("Provider - Password Recovery")]
[AllowAnonymous]
public sealed class ProviderPasswordRecoveryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProviderPasswordRecoveryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Request a recovery code by email or phone. Generic response (never reveals account existence).</summary>
    [HttpPost("forgot")]
    [ProducesResponseType(typeof(ForgotProviderPasswordResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ForgotProviderPasswordResponse>> Forgot(
        [FromBody] ForgotProviderPasswordCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Verify the OTP. Returns a short-lived reset token on success (not a login token).</summary>
    [HttpPost("otp/verify")]
    [ProducesResponseType(typeof(VerifyProviderPasswordOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderPasswordOtpResponse>> VerifyOtp(
        [FromBody] VerifyProviderPasswordOtpCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Set a new password using a valid reset token; updates Keycloak and revokes sessions.</summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetProviderPasswordResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResetProviderPasswordResponse>> Reset(
        [FromBody] ResetProviderPasswordCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Resend the OTP for an in-flight request (rate-limited).</summary>
    [HttpPost("otp/resend")]
    [ProducesResponseType(typeof(ResendProviderPasswordOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderPasswordOtpResponse>> ResendOtp(
        [FromBody] ResendProviderPasswordOtpCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
