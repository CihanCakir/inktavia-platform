namespace Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

public sealed class RequestProviderOtpLoginRequest
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}

public sealed class VerifyProviderOtpLoginRequest
{
    public string LoginRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}

public sealed class ResendProviderOtpLoginRequest
{
    public string LoginRequestId { get; set; } = default!;
}

public sealed class RequestProviderOtpLoginResponse
{
    public bool Accepted { get; set; } = true;
    public string LoginRequestId { get; set; } = string.Empty;
    public string MaskedTarget { get; set; } = string.Empty;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class VerifyProviderOtpLoginResponse
{
    public bool Verified { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public string? AuthorizationUrl { get; set; }
    public string? LoginTicket { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ConsumeTicketRequest
{
    public string Jti { get; set; } = default!;
}

public sealed class ResendProviderOtpLoginResponse
{
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
