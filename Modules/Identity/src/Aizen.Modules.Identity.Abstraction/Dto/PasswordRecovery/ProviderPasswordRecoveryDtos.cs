namespace Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

// ── Request DTOs (sent to Identity by BFF) ─────────────────────────────────

public sealed class RequestProviderPasswordRecoveryRequest
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}

public sealed class VerifyProviderPasswordRecoveryOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}

public sealed class ResetProviderPasswordRequest
{
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}

public sealed class ResendProviderPasswordRecoveryOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
}

// ── Response DTOs (returned by Identity to BFF) ────────────────────────────

public sealed class RequestProviderPasswordRecoveryResponse
{
    public bool Accepted { get; set; } = true;
    public string ResetRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class VerifyProviderPasswordRecoveryOtpResponse
{
    public bool Verified { get; set; }
    public string? ResetToken { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ResetProviderPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ResendProviderPasswordRecoveryOtpResponse
{
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
