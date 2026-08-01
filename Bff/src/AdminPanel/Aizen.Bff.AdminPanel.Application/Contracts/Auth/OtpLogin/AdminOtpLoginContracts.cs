namespace Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;

// Admin OTP → Keycloak login contracts — a byte-for-byte mirror of the MarineProvider BFF OtpLogin contracts.
// The OTP is verified by the Identity module, which mints the Keycloak token and returns a LoginTicket for the handoff.

public sealed class OtpLoginRequest { public string Channel { get; set; } = default!; public string Identifier { get; set; } = default!; }
public sealed class OtpLoginVerifyRequest { public string LoginRequestId { get; set; } = default!; public string OtpCode { get; set; } = default!; }
public sealed class OtpLoginResendRequest { public string LoginRequestId { get; set; } = default!; }

public sealed class OtpLoginRequestResponse
{
    public bool Accepted { get; set; } = true;
    public string LoginRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class OtpLoginVerifyResponse
{
    public bool Verified { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public string? LoginTicket { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class OtpLoginResendResponse
{
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
