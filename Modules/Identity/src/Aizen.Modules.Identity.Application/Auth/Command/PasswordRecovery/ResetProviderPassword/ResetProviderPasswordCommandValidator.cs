using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResetProviderPassword;

public sealed class ResetProviderPasswordCommandValidator
    : AizenValidator<ResetProviderPasswordCommand>
{
    public ResetProviderPasswordCommandValidator()
    {
        RuleFor(x => x.ResetToken).NotEmpty().MaximumLength(256);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}
