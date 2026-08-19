using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.VerifyProviderEmailVerification;

public sealed class VerifyProviderEmailVerificationCommandValidator
    : AizenValidator<VerifyProviderEmailVerificationCommand>
{
    public VerifyProviderEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}
