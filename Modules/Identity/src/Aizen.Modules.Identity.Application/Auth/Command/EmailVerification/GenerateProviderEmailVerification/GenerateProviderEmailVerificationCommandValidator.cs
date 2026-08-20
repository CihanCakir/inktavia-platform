using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.GenerateProviderEmailVerification;

public sealed class GenerateProviderEmailVerificationCommandValidator
    : AizenValidator<GenerateProviderEmailVerificationCommand>
{
    public GenerateProviderEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
    }
}
