namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.EmailVerification;

// ── Request DTO'ları (FE → BFF) ────────────────────────────────────────────

/// <summary>Birleşik token: {userId}.{Base64Url(token)} — e-postadaki linkin taşıdığı tek değer.</summary>
public sealed class VerifyEmailRequest { public string Token { get; set; } = default!; }

public sealed class ResendVerifyEmailRequest { public string Email { get; set; } = default!; }

// ── Response DTO'ları (BFF → FE) ───────────────────────────────────────────

public sealed class VerifyEmailResponse
{
    public bool Verified { get; set; }

    /// <summary>"Confirmed" | "Expired" | "Invalid" — FE bunun üzerine dallanır (yalnızca Expired'da "yeniden gönder").</summary>
    public string Status { get; set; } = "Invalid";

    public string Message { get; set; } = string.Empty;
}

public sealed class ResendVerifyEmailResponse
{
    public bool Accepted { get; set; } = true;
    public string MaskedTarget { get; set; } = string.Empty;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
