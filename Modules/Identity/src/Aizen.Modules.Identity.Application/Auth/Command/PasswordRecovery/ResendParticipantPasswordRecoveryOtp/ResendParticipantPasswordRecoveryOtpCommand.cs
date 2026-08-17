using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendParticipantPasswordRecoveryOtp;

public sealed class ResendParticipantPasswordRecoveryOtpCommand
    : AizenCommand<ResendProviderPasswordRecoveryOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
}
