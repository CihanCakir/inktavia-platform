using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Validators;

public sealed class UpdateExchangeRateCommandValidator : AizenValidator<UpdateExchangeRateCommand>
{
    public UpdateExchangeRateCommandValidator()
    {
        RuleFor(x => x.Request.FromCurrencyCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.ToCurrencyCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Request.Rate).GreaterThan(0);
        RuleFor(x => x.Request.RateDate).NotEmpty();
        RuleFor(x => x.Request.ValidUntil).NotEmpty();
    }
}
