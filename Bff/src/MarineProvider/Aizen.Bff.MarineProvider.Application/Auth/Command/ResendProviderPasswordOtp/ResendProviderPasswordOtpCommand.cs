using Aizen.Bff.MarineProvider.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>Public: resend the recovery OTP for an in-flight request (rate-limited by the resend cooldown).</summary>
public sealed class ResendProviderPasswordOtpCommand : AizenCommand<ResendProviderPasswordOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
}
