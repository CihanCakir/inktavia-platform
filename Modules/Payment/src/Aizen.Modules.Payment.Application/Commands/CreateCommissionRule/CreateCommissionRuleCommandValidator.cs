using Aizen.Modules.Payment.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommandValidator
    : AbstractValidator<CreateCommissionRuleCommand>
{
    public CreateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("CommissionRate must be between 0 and 100.");

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
    }
}
