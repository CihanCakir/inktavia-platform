using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class RegisterProviderCommandValidator : AizenValidator<RegisterProviderCommand>
{
    public RegisterProviderCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OwnerFirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.OwnerLastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.KvkkAccepted).Equal(true);
        RuleFor(x => x.ContactPhone).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
        RuleFor(x => x.TaxNo).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.TaxNo));
    }
}
