using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendParticipantPasswordRecoveryOtp;

public sealed class ResendParticipantPasswordRecoveryOtpCommandValidator
    : AizenValidator<ResendParticipantPasswordRecoveryOtpCommand>
{
    public ResendParticipantPasswordRecoveryOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(64);
    }
}
