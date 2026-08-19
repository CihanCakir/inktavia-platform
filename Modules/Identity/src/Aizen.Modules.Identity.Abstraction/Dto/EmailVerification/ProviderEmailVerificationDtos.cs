namespace Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

// ── Request DTO'ları (BFF → Identity) ──────────────────────────────────────

public sealed class GenerateProviderEmailVerificationRequest
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
}

public sealed class VerifyProviderEmailVerificationRequest
{
    public string Token { get; set; } = default!;
}

public sealed class ConsumeProviderEmailVerificationRequest
{
    public string Token { get; set; } = default!;
}

public sealed class ResendProviderEmailVerificationRequest
{
    public string Email { get; set; } = default!;
}

// ── Response DTO'ları (Identity → BFF) ─────────────────────────────────────

public sealed class GenerateProviderEmailVerificationResponse
{
    public bool Accepted { get; set; } = true;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class VerifyProviderEmailVerificationResponse
{
    public bool Verified { get; set; }
    public long? UserId { get; set; }
    public string? KeycloakSubjectId { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ConsumeProviderEmailVerificationResponse
{
    public bool Consumed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ResendProviderEmailVerificationResponse
{
    public bool Accepted { get; set; } = true;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
