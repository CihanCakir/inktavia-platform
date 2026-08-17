namespace Aizen.Modules.Identity.Domain.Model.OtpLogin;

public sealed class OtpLoginRequestResult
{
    public string LoginRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
}

public sealed class OtpLoginVerifyResult
{
    public bool Verified { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public string? AuthorizationUrl { get; set; }
    public string? LoginTicket { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Resolution of a login identifier (email|phone) to the canonical participant used for a Keycloak ROPC:
/// the Keycloak username IS the email, and phone is not stored in Keycloak — so phone password-login must
/// resolve to this email first. Null resolution = no active participant matches (→ invalid credentials).
/// </summary>
public sealed class ParticipantIdentifierResolution
{
    public string Email { get; set; } = string.Empty;
    public string KeycloakSubjectId { get; set; } = string.Empty;
    public long ParticipantProfileId { get; set; }
}

public sealed class OtpLoginResendResult
{
    /// <summary>False when the cooldown has not elapsed: no new OTP was generated or dispatched.</summary>
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = "If an account exists, a new verification code has been sent.";
}
