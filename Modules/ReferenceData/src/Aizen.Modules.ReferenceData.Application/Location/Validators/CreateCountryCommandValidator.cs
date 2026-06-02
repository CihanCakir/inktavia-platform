using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Location.Commands;

namespace Aizen.Modules.ReferenceData.Application.Location.Validators;

public sealed class CreateCountryCommandValidator : AizenValidator<CreateCountryCommand>
{
    public CreateCountryCommandValidator()
    {
        RuleFor(x => x.Request.CountryCode).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Request.NumericCode).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Request.Name).NotEmpty();
        RuleFor(x => x.Request.DefaultCurrencyCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.PhoneCode).NotEmpty().MaximumLength(10);
    }
}
