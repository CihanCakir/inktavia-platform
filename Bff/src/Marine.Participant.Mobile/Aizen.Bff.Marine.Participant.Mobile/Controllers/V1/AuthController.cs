using Aizen.Bff.Marine.Participant.Mobile.Application.Auth;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// Mobile participant OTP login. <c>send</c> mints an OTP (Identity), the app carries the returned
/// <c>loginRequestId</c> into <c>verify</c>, and <c>verify</c> returns REAL Keycloak tokens via the
/// native ticket→session handoff (server-side auth-code + PKCE against inktavia-mobile).
/// </summary>
[ApiController]
[Route("api/v1/mobile/auth/otp")]
[Tags("Mobile - Auth OTP")]
[AllowAnonymous]
[EnableRateLimiting("pwd-recovery-ip")]
public sealed class AuthController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AuthController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) { _cqrs = cqrs; }

    /// <summary>Request an OTP for a phone or email identifier. Returns the loginRequestId to carry to verify.</summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(MobileOtpSendResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileOtpSendResponse>> Send(
        [FromBody] MobileOtpSendRequest request, CancellationToken ct)
    {
        // Derive the Identity channel from the identifier shape. Identity's participant OTP-login keys on
        // "email"/"phone" (see M2a domain service), so an '@' means email, otherwise phone.
        var identifier = (request.Identifier ?? string.Empty).Trim();
        var channel = identifier.Contains('@') ? "email" : "phone";

        var command = new RequestParticipantOtpLoginCommand { Channel = channel, Identifier = identifier };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Verify the OTP and complete sign-in — returns real Keycloak access/refresh tokens.</summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(MobileOtpVerifyResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileOtpVerifyResponse>> Verify(
        [FromBody] MobileOtpVerifyRequest request, CancellationToken ct)
    {
        var command = new VerifyParticipantOtpLoginCommand
        { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Resend the OTP for an in-flight loginRequestId (honors the Identity cooldown).</summary>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(MobileOtpResendResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileOtpResendResponse>> Resend(
        [FromBody] MobileOtpResendRequest request, CancellationToken ct)
    {
        var command = new ResendParticipantOtpLoginCommand { LoginRequestId = request.LoginRequestId };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    // ── Password account paths (absolute routes → /api/v1/mobile/auth/*) ──────────────────────────

    /// <summary>Create an account (Keycloak user + linked Participant profile) and return a session.</summary>
    [HttpPost("/api/v1/mobile/auth/register")]
    [ProducesResponseType(typeof(MobileAuthTokenResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileAuthTokenResponse>> Register(
        [FromBody] MobileRegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterParticipantCommand
        {
            Email = request.Email, Password = request.Password,
            FullName = request.FullName, Phone = request.Phone
        };
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Email + password login → real Keycloak (inktavia-mobile) tokens. Invalid creds → 401.</summary>
    [HttpPost("/api/v1/mobile/auth/login")]
    [ProducesResponseType(typeof(MobileAuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new LoginParticipantCommand { Email = request.Email, Password = request.Password }, ct);
        return result is null ? Unauthorized(Fail("Invalid email or password.")) : Ok(SetResponse(result));
    }

    /// <summary>Exchange a refresh token for new tokens. Invalid/expired → 401.</summary>
    [HttpPost("/api/v1/mobile/auth/refresh")]
    [ProducesResponseType(typeof(MobileAuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] MobileRefreshRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RefreshParticipantCommand { RefreshToken = request.RefreshToken }, ct);
        return result is null ? Unauthorized(Fail("Session expired. Please sign in again.")) : Ok(SetResponse(result));
    }

    /// <summary>Revoke the refresh token / Keycloak session. Idempotent → 200.</summary>
    [HttpPost("/api/v1/mobile/auth/logout")]
    [ProducesResponseType(typeof(MobileLogoutResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileLogoutResponse>> Logout(
        [FromBody] MobileLogoutRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new LogoutParticipantCommand { RefreshToken = request.RefreshToken }, ct);
        return SetResponse(result);
    }

    private static AizenApiResponse<MobileAuthTokenResponse> Fail(string message) =>
        new(AizenResponseHeader.Fail(new AizenBusinessException(message)), null!);
}
