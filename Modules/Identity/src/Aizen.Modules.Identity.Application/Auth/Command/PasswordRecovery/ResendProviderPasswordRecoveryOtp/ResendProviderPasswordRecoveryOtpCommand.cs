using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendProviderPasswordRecoveryOtp;

public sealed class ResendProviderPasswordRecoveryOtpCommand
    : AizenCommand<ResendProviderPasswordRecoveryOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
}
