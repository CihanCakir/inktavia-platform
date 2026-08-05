namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;

// ── Request DTOs (client → mobile BFF) ─────────────────────────────────────

/// <summary>Body for POST /api/v1/mobile/auth/forgot-password. The channel (email/phone) is derived
/// from the identifier shape server-side, mirroring the OTP-login send endpoint.</summary>
public sealed class MobileForgotPasswordRequest
{
    public string Identifier { get; set; } = default!;
}

public sealed class MobileVerifyPasswordOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}

public sealed class MobileSetNewPasswordRequest
{
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}

public sealed class MobileResendPasswordOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
}

// ── Response DTOs (mobile BFF → client) ────────────────────────────────────

/// <summary>Anti-enumeration: always returned (even for unknown identifiers).</summary>
public sealed class MobileForgotPasswordResponse
{
    public string ResetRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public sealed class MobileVerifyPasswordOtpResponse
{
    public string? ResetToken { get; set; }
}

public sealed class MobileSetNewPasswordResponse
{
    public bool Success { get; set; }
}

public sealed class MobileResendPasswordOtpResponse
{
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
}
