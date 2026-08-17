namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;

/// <summary>
/// Generic (non-enumeration) response to a password recovery request. Fields are populated regardless of whether
/// an account exists so callers cannot infer account existence; when no account matches, a synthetic
/// resetRequestId is returned and no OTP is actually generated.
/// </summary>
public sealed class ForgotProviderPasswordResponse
{
    public bool Accepted { get; set; } = true;
    public string ResetRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
