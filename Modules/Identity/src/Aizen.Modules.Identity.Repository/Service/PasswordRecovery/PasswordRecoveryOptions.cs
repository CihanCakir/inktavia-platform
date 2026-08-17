namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

public sealed class PasswordRecoveryOptions
{
    public const string SectionName = "PasswordRecovery";

    public int OtpLength { get; set; } = 6;
    public int OtpTtlSeconds { get; set; } = 300;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int ResetTokenTtlSeconds { get; set; } = 300;

    /// <summary>
    /// <c>Notification</c> (default) publishes to the message bus for real email/SMS delivery.
    /// <c>Logging</c> logs delivery intent only (local development).
    /// </summary>
    public string DeliveryMode { get; set; } = "Notification";

    /// <summary>
    /// DEV-ONLY. When true, the OTP is written to logs at Debug level so a developer can complete the flow
    /// locally without a wired Notification/SMS/email provider. MUST remain false in shared/test/production.
    /// </summary>
    public bool DevExposeOtp { get; set; } = false;

    /// <summary>Maximum forgot/request calls per identifier within the sliding window. 0 = disabled.</summary>
    public int MaxRequestsPerIdentifierPerWindow { get; set; } = 5;

    /// <summary>Window duration in seconds for the per-identifier rate limit.</summary>
    public int IdentifierWindowSeconds { get; set; } = 3600;
}
