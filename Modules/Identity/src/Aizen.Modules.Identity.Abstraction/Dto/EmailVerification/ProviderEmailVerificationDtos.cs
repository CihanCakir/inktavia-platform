namespace Aizen.Modules.Identity.Abstraction.Dto.EmailVerification;

// ── Request DTO'ları (BFF → Identity) ──────────────────────────────────────

public sealed class GenerateProviderEmailVerificationRequest
{
    public string Email { get; set; } = default!;
}

/// <summary>Birleşik token: {userId}.{Base64Url(token)} — tek dizedir (BFF/FE için tek parametre).</summary>
public sealed class ConfirmProviderEmailVerificationRequest
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

public sealed class ConfirmProviderEmailVerificationResponse
{
    public bool Confirmed { get; set; }

    /// <summary>"Confirmed" | "Expired" | "Invalid" — FE bunun üzerine dallanır (yalnızca Expired'da "yeniden gönder").</summary>
    public string Status { get; set; } = "Invalid";

    public long? UserId { get; set; }
    public string? KeycloakSubjectId { get; set; }
    public string? Email { get; set; }
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
