using Aizen.Modules.Payment.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateCustomerDiscountRule;

public sealed class CreateCustomerDiscountRuleCommandValidator
    : AbstractValidator<CreateCustomerDiscountRuleCommand>
{
    public CreateCustomerDiscountRuleCommandValidator()
    {
        RuleFor(x => x.CurrencyCode).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");

        RuleFor(x => x.DiscountRate)
            .NotNull().WithMessage("Percent discount requires a DiscountRate.")
            .When(x => x.DiscountType == CustomerDiscountType.Percent);
        RuleFor(x => x.DiscountRate!.Value)
            .ExclusiveBetween(0m, 1m).When(x => x.DiscountType == CustomerDiscountType.Percent && x.DiscountRate.HasValue)
            .WithMessage("DiscountRate must be a fraction in (0,1).");

        RuleFor(x => x.FixedDiscountAmount)
            .NotNull().GreaterThan(0m).WithMessage("Fixed discount requires a positive FixedDiscountAmount.")
            .When(x => x.DiscountType == CustomerDiscountType.Fixed);

        When(x => x.FundingMode == CustomerDiscountFundingMode.Shared, () =>
        {
            RuleFor(x => x.PlatformFundingRate).NotNull();
            RuleFor(x => x.ProviderFundingRate).NotNull();
            RuleFor(x => x)
                .Must(x => (x.PlatformFundingRate ?? -1) + (x.ProviderFundingRate ?? -1) == 1.0m)
                .WithMessage("Shared funding rates must sum to 1.0 (100%).");
        });
    }
}
