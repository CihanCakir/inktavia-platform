using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyParticipantPasswordRecoveryOtp;

public sealed class VerifyParticipantPasswordRecoveryOtpCommand
    : AizenCommand<VerifyProviderPasswordRecoveryOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
