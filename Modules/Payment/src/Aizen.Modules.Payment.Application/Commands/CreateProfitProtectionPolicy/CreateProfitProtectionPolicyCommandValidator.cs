using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateProfitProtectionPolicy;

public sealed class CreateProfitProtectionPolicyCommandValidator
    : AbstractValidator<CreateProfitProtectionPolicyCommand>
{
    public CreateProfitProtectionPolicyCommandValidator()
    {
        RuleFor(x => x.CurrencyCode).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");

        RuleFor(x => x.CustomerSideVariableCostShareRate)
            .InclusiveBetween(0m, 1m)
            .WithMessage("CustomerSideVariableCostShareRate must be within [0, 1].");

        // All thresholds must be non-negative (rates are fractions; amounts are money).
        RuleFor(x => x.MinCustomerSideContributionAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinCustomerSideContributionRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinProviderSideContributionAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinProviderSideContributionRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinTransactionContributionAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinTransactionContributionRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.PaymentProcessingExpenseRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.PaymentProcessingFixed).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.RefundRiskReserveRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.OtherVariableExpenseRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.OtherVariableExpenseFixed).GreaterThanOrEqualTo(0m);
    }
}
