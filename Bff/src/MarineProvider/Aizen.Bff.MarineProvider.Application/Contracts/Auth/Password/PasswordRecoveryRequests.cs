namespace Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;

// Provider-facing HTTP request contracts for the password recovery endpoints. Kept separate from the internal CQRS
// command classes so command types are never exposed as HTTP contracts. Wire shape is unchanged (frontend-safe).

public sealed class ForgotProviderPasswordRequest
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}

public sealed class VerifyProviderPasswordOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}

public sealed class ResetProviderPasswordRequest
{
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}

public sealed class ResendProviderPasswordOtpRequest
{
    public string ResetRequestId { get; set; } = default!;
}
