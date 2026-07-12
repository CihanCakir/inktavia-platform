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

public sealed class OtpLoginResendResult
{
    /// <summary>False when the cooldown has not elapsed: no new OTP was generated or dispatched.</summary>
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = "If an account exists, a new verification code has been sent.";
}
