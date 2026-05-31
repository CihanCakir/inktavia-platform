using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Currency.Commands;

namespace Aizen.Modules.ReferenceData.Application.Currency.Validators;

public sealed class CreateCurrencyCommandValidator : AizenValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.NumericCode).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.DecimalPlaces).InclusiveBetween(0, 8);
    }
}
