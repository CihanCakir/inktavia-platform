using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.MarineProvider.Controllers.V1.Auth;

/// <summary>
/// Provider-facing password recovery façade (public/anonymous). The BFF delegates all recovery logic to the Identity
/// module (Identity owns OTP/reset state; Keycloak owns the password). No login token is issued; OTP is for reset
/// only. Thin controller — endpoints bind HTTP <c>Request</c> DTOs and map them to internal CQRS commands.
/// </summary>
[ApiController]
[Route("api/v1/provider/auth/password")]
[Tags("Provider - Password Recovery")]
[AllowAnonymous]
[EnableRateLimiting("pwd-recovery-ip")]
public sealed class PasswordRecoveryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public PasswordRecoveryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Request a recovery code by email or phone. Generic response (never reveals account existence).</summary>
    [HttpPost("forgot")]
    [ProducesResponseType(typeof(ForgotProviderPasswordResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ForgotProviderPasswordResponse>> Forgot(
        [FromBody] ForgotProviderPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotProviderPasswordCommand
        {
            Channel = request.Channel,
            Identifier = request.Identifier,
        };

        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Verify the OTP. Returns a short-lived reset token on success (not a login token).</summary>
    [HttpPost("otp/verify")]
    [ProducesResponseType(typeof(VerifyProviderPasswordOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderPasswordOtpResponse>> VerifyOtp(
        [FromBody] VerifyProviderPasswordOtpRequest request, CancellationToken ct)
    {
        var command = new VerifyProviderPasswordOtpCommand
        {
            ResetRequestId = request.ResetRequestId,
            OtpCode = request.OtpCode,
        };

        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Set a new password using a valid reset token; Identity updates Keycloak and revokes sessions.</summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetProviderPasswordResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResetProviderPasswordResponse>> Reset(
        [FromBody] ResetProviderPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetProviderPasswordCommand
        {
            ResetToken = request.ResetToken,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword,
        };

        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Resend the OTP for an in-flight request (rate-limited).</summary>
    [HttpPost("otp/resend")]
    [ProducesResponseType(typeof(ResendProviderPasswordOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderPasswordOtpResponse>> ResendOtp(
        [FromBody] ResendProviderPasswordOtpRequest request, CancellationToken ct)
    {
        var command = new ResendProviderPasswordOtpCommand
        {
            ResetRequestId = request.ResetRequestId,
        };

        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
