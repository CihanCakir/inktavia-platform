using Aizen.Modules.Payment.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.UpdatePlatformFeeRule;

public sealed class UpdatePlatformFeeRuleCommandValidator
    : AbstractValidator<UpdatePlatformFeeRuleCommand>
{
    public UpdatePlatformFeeRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than zero.");

        RuleFor(x => x.EffectiveFrom).NotEmpty().WithMessage("EffectiveFrom is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");

        RuleFor(x => x.Rate!.Value)
            .ExclusiveBetween(0m, 1m)
            .When(x => x.Rate.HasValue)
            .WithMessage("Rate must be a fraction strictly between 0 and 1 (e.g. 0.025 = 2.5%).");

        RuleFor(x => x.VatRate!.Value)
            .InclusiveBetween(0m, 1m)
            .When(x => x.VatRate.HasValue)
            .WithMessage("VatRate must be between 0 and 1.");

        RuleFor(x => x.Rate)
            .NotNull().WithMessage("Percentage model requires a Rate.")
            .When(x => x.Model == PlatformFeeModel.Percentage);

        RuleFor(x => x.FixedAmount)
            .NotNull().GreaterThanOrEqualTo(0m).WithMessage("Fixed model requires a non-negative FixedAmount.")
            .When(x => x.Model == PlatformFeeModel.Fixed);

        When(x => x.Model == PlatformFeeModel.PercentageWithBounds, () =>
        {
            RuleFor(x => x.Rate).NotNull().WithMessage("PercentageWithBounds model requires a Rate.");
            RuleFor(x => x.MinAmount).NotNull().WithMessage("PercentageWithBounds model requires MinAmount.");
            RuleFor(x => x.MaxAmount).NotNull().WithMessage("PercentageWithBounds model requires MaxAmount.");
            RuleFor(x => x)
                .Must(x => !(x.MinAmount.HasValue && x.MaxAmount.HasValue) || x.MinAmount <= x.MaxAmount)
                .WithMessage("MinAmount must be ≤ MaxAmount.");
        });
    }
}
