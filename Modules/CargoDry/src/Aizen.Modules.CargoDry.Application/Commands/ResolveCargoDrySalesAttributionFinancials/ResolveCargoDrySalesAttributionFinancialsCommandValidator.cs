using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;

public sealed class ResolveCargoDrySalesAttributionFinancialsCommandValidator
    : AbstractValidator<ResolveCargoDrySalesAttributionFinancialsCommand>
{
    public ResolveCargoDrySalesAttributionFinancialsCommandValidator()
    {
        RuleFor(x => x.SalesAttributionId)
            .GreaterThan(0).WithMessage("SalesAttributionId must be a valid entity Id.");

        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("SalePrice must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be an ISO 4217 3-letter code.");

        When(x => x.CommissionRateOverride.HasValue, () =>
        {
            RuleFor(x => x.CommissionRateOverride!.Value)
                .InclusiveBetween(0m, 1m)
                .WithMessage("CommissionRateOverride must be between 0.00 and 1.00.");
        });

        RuleFor(x => x.ResolvedByUserId)
            .GreaterThan(0).WithMessage("ResolvedByUserId must be a valid user Id.");

        RuleFor(x => x.ResolutionNote)
            .MaximumLength(1000).WithMessage("ResolutionNote must not exceed 1000 characters.")
            .When(x => x.ResolutionNote is not null);
    }
}
