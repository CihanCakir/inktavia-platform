using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.ForgotProviderPassword;

public sealed class ForgotProviderPasswordCommandValidator : AizenValidator<ForgotProviderPasswordCommand>
{
    public ForgotProviderPasswordCommandValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(c => c is "email" or "phone")
            .WithMessage("Channel must be 'email' or 'phone'.");

        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256);

        When(x => x.Channel == "email", () =>
        {
            RuleFor(x => x.Identifier).EmailAddress();
        });

        When(x => x.Channel == "phone", () =>
        {
            RuleFor(x => x.Identifier).Matches(@"^\+?\d{8,15}$").WithMessage("Enter a valid phone number.");
        });
    }
}
