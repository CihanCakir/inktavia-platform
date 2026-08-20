using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConfirmProviderEmailVerification;

public sealed class ConfirmProviderEmailVerificationCommandValidator
    : AizenValidator<ConfirmProviderEmailVerificationCommand>
{
    public ConfirmProviderEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(1024);
    }
}
