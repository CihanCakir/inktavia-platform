namespace Aizen.Bff.MarineProvider.Application.Common.Options;

/// <summary>
/// Tunables for the BFF-orchestrated provider password recovery flow.
/// Bound from configuration section "PasswordRecovery".
/// </summary>
public sealed class PasswordRecoveryOptions
{
    public const string SectionName = "PasswordRecovery";

    /// <summary>Number of digits in the OTP.</summary>
    public int OtpLength { get; set; } = 6;

    /// <summary>OTP validity window (seconds).</summary>
    public int OtpTtlSeconds { get; set; } = 300;

    /// <summary>Minimum wait between OTP (re)sends (seconds).</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Maximum OTP verification attempts before the request is invalidated.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Validity window of the short-lived reset token issued after OTP verification (seconds).</summary>
    public int ResetTokenTtlSeconds { get; set; } = 300;

    /// <summary>
    /// DEV-ONLY. When true, the OTP is written to logs at Debug level so a developer can complete the flow
    /// locally without a wired Notification/SMS/email provider. MUST remain false in shared/test/production.
    /// </summary>
    public bool DevExposeOtp { get; set; } = false;
}
