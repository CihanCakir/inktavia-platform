using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Currency.Commands;

namespace Aizen.Modules.ReferenceData.Application.Currency.Validators;

public sealed class UpdateCurrencyCommandValidator : AizenValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 8);
    }
}
