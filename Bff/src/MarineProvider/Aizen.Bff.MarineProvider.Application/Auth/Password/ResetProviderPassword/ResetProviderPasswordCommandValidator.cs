using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ResetProviderPassword;

public sealed class ResetProviderPasswordCommandValidator : AizenValidator<ResetProviderPasswordCommand>
{
    public ResetProviderPasswordCommandValidator()
    {
        RuleFor(x => x.ResetToken).NotEmpty().MaximumLength(512);

        // Baseline password policy (server-side source of truth; Keycloak may enforce stricter).
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[\d!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]").WithMessage("Password must contain a number or symbol.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords must match.");
    }
}
