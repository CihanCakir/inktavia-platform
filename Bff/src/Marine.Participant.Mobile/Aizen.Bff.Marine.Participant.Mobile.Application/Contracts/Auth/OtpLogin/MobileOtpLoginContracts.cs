namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;

/// <summary>Mobile-facing OTP-login contracts. The native app carries <c>loginRequestId</c> from send→verify
/// (no BFF-side correlation state), exactly as Identity's OTP-login is keyed.</summary>

// ── Requests (mobile app → BFF) ───────────────────────────────────────────────

/// <summary>Single identifier (phone or email); the BFF derives the channel.</summary>
public sealed class MobileOtpSendRequest
{
    public string Identifier { get; set; } = default!;
}

public sealed class MobileOtpVerifyRequest
{
    public string LoginRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}

public sealed class MobileOtpResendRequest
{
    public string LoginRequestId { get; set; } = default!;
}

// ── Responses (BFF → mobile app) ──────────────────────────────────────────────

public sealed class MobileOtpSendResponse
{
    public string LoginRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

/// <summary>Verify success returns real Keycloak tokens (native ticket→session handoff completed).</summary>
public sealed class MobileOtpVerifyResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public sealed class MobileOtpResendResponse
{
    public bool Resent { get; set; }
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
