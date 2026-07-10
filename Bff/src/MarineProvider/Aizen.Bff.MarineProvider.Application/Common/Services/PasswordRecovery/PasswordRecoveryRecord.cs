namespace Aizen.Bff.MarineProvider.Application.Common.Services.PasswordRecovery;

/// <summary>
/// Server-side password recovery request state (persisted in the distributed cache, never in the browser).
/// Stores only hashes of the OTP and reset token — never the raw OTP, raw token, or any password.
/// </summary>
public sealed class PasswordRecoveryRecord
{
    public string ResetRequestId { get; set; } = default!;
    public string KeycloakUserId { get; set; } = default!;
    public long? ProviderProfileId { get; set; }

    /// <summary>"email" or "phone".</summary>
    public string Channel { get; set; } = default!;

    /// <summary>Hash of the normalized target (e.g. email), for optional correlation. Never the raw value.</summary>
    public string TargetHash { get; set; } = default!;
    public string MaskedTarget { get; set; } = default!;

    public string OtpHash { get; set; } = default!;
    public string OtpSalt { get; set; } = default!;
    public DateTime OtpExpiresAtUtc { get; set; }

    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public string? ResetTokenHash { get; set; }
    public string? ResetTokenSalt { get; set; }
    public DateTime? ResetTokenExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
}
