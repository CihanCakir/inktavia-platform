using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.PasswordRecovery;

public class ParticipantPasswordRecoveryRequestEntity : AizenEntityWithAudit
{
    public string ResetRequestId { get; set; } = default!;
    public string KeycloakSubjectId { get; set; } = default!;
    public long UserId { get; set; }
    public long? ParticipantProfileId { get; set; }

    /// <summary>"email" or "phone".</summary>
    public string Channel { get; set; } = default!;

    /// <summary>Hash of the normalized target (email/phone), for optional correlation.</summary>
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

    public DateTime LastSentAtUtc { get; set; }

    protected ParticipantPasswordRecoveryRequestEntity() { }

    public static ParticipantPasswordRecoveryRequestEntity Create(
        string resetRequestId,
        string keycloakSubjectId,
        long userId,
        long? participantProfileId,
        string channel,
        string targetHash,
        string maskedTarget,
        string otpHash,
        string otpSalt,
        DateTime otpExpiresAtUtc,
        int maxAttempts)
    {
        var now = DateTime.UtcNow;
        return new ParticipantPasswordRecoveryRequestEntity
        {
            ResetRequestId = resetRequestId,
            KeycloakSubjectId = keycloakSubjectId,
            UserId = userId,
            ParticipantProfileId = participantProfileId,
            Channel = channel,
            TargetHash = targetHash,
            MaskedTarget = maskedTarget,
            OtpHash = otpHash,
            OtpSalt = otpSalt,
            OtpExpiresAtUtc = otpExpiresAtUtc,
            Attempts = 0,
            MaxAttempts = maxAttempts,
            LastSentAtUtc = now,
            CreateDate = now,
        };
    }

    public void MarkConsumed() => ConsumedAtUtc = DateTime.UtcNow;

    public void IncrementAttempt() => Attempts++;

    public bool IsOtpExpired() => OtpExpiresAtUtc <= DateTime.UtcNow;

    public void SetResetToken(string tokenHash, string tokenSalt, DateTime expiresAtUtc)
    {
        ResetTokenHash = tokenHash;
        ResetTokenSalt = tokenSalt;
        ResetTokenExpiresAtUtc = expiresAtUtc;
    }
}
