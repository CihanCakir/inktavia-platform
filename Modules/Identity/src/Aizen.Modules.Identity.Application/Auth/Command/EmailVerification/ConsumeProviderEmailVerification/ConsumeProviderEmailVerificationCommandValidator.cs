using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ConsumeProviderEmailVerification;

public sealed class ConsumeProviderEmailVerificationCommandValidator
    : AizenValidator<ConsumeProviderEmailVerificationCommand>
{
    public ConsumeProviderEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}
