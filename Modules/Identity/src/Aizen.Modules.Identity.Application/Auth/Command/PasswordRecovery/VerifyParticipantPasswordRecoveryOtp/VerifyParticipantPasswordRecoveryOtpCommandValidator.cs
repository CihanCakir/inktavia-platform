using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyParticipantPasswordRecoveryOtp;

public sealed class VerifyParticipantPasswordRecoveryOtpCommandValidator
    : AizenValidator<VerifyParticipantPasswordRecoveryOtpCommand>
{
    public VerifyParticipantPasswordRecoveryOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OtpCode).NotEmpty().MaximumLength(10);
    }
}
