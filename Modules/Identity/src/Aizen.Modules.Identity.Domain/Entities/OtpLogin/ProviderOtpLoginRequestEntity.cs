using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.OtpLogin;

public class ProviderOtpLoginRequestEntity : AizenEntityWithAudit
{
    public string LoginRequestId { get; set; } = default!;
    public string KeycloakSubjectId { get; set; } = default!;
    public long UserId { get; set; }
    public long? ProviderProfileId { get; set; }
    public string Channel { get; set; } = default!;
    public string TargetHash { get; set; } = default!;
    public string MaskedTarget { get; set; } = default!;
    public string OtpHash { get; set; } = default!;
    public string OtpSalt { get; set; } = default!;
    public DateTime OtpExpiresAtUtc { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }

    protected ProviderOtpLoginRequestEntity() { }

    public static ProviderOtpLoginRequestEntity Create(
        string loginRequestId, string keycloakSubjectId, long userId, long? providerProfileId,
        string channel, string targetHash, string maskedTarget,
        string otpHash, string otpSalt, DateTime otpExpiresAtUtc, int maxAttempts)
    {
        var now = DateTime.UtcNow;
        return new ProviderOtpLoginRequestEntity
        {
            LoginRequestId = loginRequestId, KeycloakSubjectId = keycloakSubjectId,
            UserId = userId, ProviderProfileId = providerProfileId,
            Channel = channel, TargetHash = targetHash, MaskedTarget = maskedTarget,
            OtpHash = otpHash, OtpSalt = otpSalt, OtpExpiresAtUtc = otpExpiresAtUtc,
            Attempts = 0, MaxAttempts = maxAttempts, LastSentAtUtc = now, CreateDate = now,
        };
    }

    public void MarkConsumed() => ConsumedAtUtc = DateTime.UtcNow;
    public void IncrementAttempt() => Attempts++;
    public bool IsOtpExpired() => OtpExpiresAtUtc <= DateTime.UtcNow;
}
