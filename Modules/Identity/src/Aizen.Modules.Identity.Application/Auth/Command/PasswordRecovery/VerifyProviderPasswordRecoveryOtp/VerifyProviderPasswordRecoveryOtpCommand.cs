using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyProviderPasswordRecoveryOtp;

public sealed class VerifyProviderPasswordRecoveryOtpCommand
    : AizenCommand<VerifyProviderPasswordRecoveryOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
