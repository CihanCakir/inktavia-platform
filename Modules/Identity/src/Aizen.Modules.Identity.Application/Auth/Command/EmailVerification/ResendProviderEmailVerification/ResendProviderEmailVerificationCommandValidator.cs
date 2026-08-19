using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.EmailVerification.ResendProviderEmailVerification;

public sealed class ResendProviderEmailVerificationCommandValidator
    : AizenValidator<ResendProviderEmailVerificationCommand>
{
    public ResendProviderEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
    }
}
