using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyProviderPasswordRecoveryOtp;

public sealed class VerifyProviderPasswordRecoveryOtpCommandValidator
    : AizenValidator<VerifyProviderPasswordRecoveryOtpCommand>
{
    public VerifyProviderPasswordRecoveryOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OtpCode).NotEmpty().MaximumLength(10);
    }
}
