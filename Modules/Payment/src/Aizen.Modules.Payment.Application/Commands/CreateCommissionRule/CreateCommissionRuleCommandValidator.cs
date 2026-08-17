using Aizen.Modules.Payment.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommandValidator
    : AbstractValidator<CreateCommissionRuleCommand>
{
    public CreateCommissionRuleCommandValidator()
    {
        // §6: rate is a fraction in the open interval (0,1) — e.g. 0.12 = 12%.
        RuleFor(x => x.CommissionRate)
            .ExclusiveBetween(0m, 1m)
            .WithMessage("CommissionRate must be a fraction strictly between 0 and 1 (e.g. 0.12 = 12%).");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("EffectiveFrom is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null)
            .WithMessage("Notes must not exceed 1000 characters.");

        // Category rule requires a CategoryCode
        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("CategoryCode is required for Category rules.")
            .When(x => x.RuleType == CommissionRuleType.Category);

        // Plan rule requires a ProviderPlanId
        RuleFor(x => x.ProviderPlanId)
            .GreaterThan(0).WithMessage("ProviderPlanId must be greater than zero for Plan rules.")
            .When(x => x.RuleType == CommissionRuleType.Plan);

        // ProviderOverride rule requires a ProviderProfileId
        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be greater than zero for ProviderOverride rules.")
            .When(x => x.RuleType == CommissionRuleType.ProviderOverride);

        // §6: Global rule must not declare any primary scope dimension.
        RuleFor(x => x)
            .Must(x => x.ProviderPlanId is null && x.ProviderProfileId is null && string.IsNullOrWhiteSpace(x.CategoryCode))
            .When(x => x.RuleType == CommissionRuleType.Global)
            .WithMessage("Global rules must not set ProviderPlanId, ProviderProfileId, or CategoryCode.");
    }
}
