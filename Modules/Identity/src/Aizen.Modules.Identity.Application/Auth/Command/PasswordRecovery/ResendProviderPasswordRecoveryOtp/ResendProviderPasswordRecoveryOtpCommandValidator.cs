using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendProviderPasswordRecoveryOtp;

public sealed class ResendProviderPasswordRecoveryOtpCommandValidator
    : AizenValidator<ResendProviderPasswordRecoveryOtpCommand>
{
    public ResendProviderPasswordRecoveryOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(64);
    }
}
