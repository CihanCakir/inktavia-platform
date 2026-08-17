namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;

/// <summary>Result of an OTP resend. Generic response; rate-limited by the resend cooldown.</summary>
public sealed class ResendProviderPasswordOtpResponse
{
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
