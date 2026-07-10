namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;

/// <summary>
/// Result of OTP verification. On success returns a short-lived reset token authorizing exactly one password
/// change. This is NOT a login token and carries no Keycloak session.
/// </summary>
public sealed class VerifyProviderPasswordOtpResponse
{
    public bool Verified { get; set; }
    public string? ResetToken { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
