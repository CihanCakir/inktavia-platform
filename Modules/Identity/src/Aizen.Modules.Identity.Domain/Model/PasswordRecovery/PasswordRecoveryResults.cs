namespace Aizen.Modules.Identity.Domain.Model.PasswordRecovery;

/// <summary>
/// Domain-internal result models returned by <c>IProviderPasswordRecoveryDomainService</c>. These are NOT the
/// cross-boundary HTTP contracts — Application handlers map them to the Abstraction response DTOs
/// (Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery). Kept in Domain so Domain does not depend on
/// Abstraction; the Domain → Abstraction mapping lives in the Application layer.
/// </summary>
public sealed class PasswordRecoveryRequestResult
{
    public string ResetRequestId { get; set; } = default!;
    public string MaskedTarget { get; set; } = default!;
    public int OtpLength { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAfterSeconds { get; set; }
}

public sealed class PasswordRecoveryVerifyResult
{
    public bool Verified { get; set; }
    public string? ResetToken { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class PasswordRecoveryResetResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
}

public sealed class PasswordRecoveryResendResult
{
    public bool Resent { get; set; } = true;
    public int ResendAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
}
