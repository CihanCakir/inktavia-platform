using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.VerifyProviderPasswordOtp;

/// <summary>Public: verify the recovery OTP. On success returns a short-lived reset token (not a login token).</summary>
public sealed class VerifyProviderPasswordOtpCommand : AizenCommand<VerifyProviderPasswordOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
