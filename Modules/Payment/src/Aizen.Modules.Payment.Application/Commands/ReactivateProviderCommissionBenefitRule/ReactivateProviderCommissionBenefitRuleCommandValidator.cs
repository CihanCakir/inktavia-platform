using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProviderCommissionBenefitRule;

public sealed class ReactivateProviderCommissionBenefitRuleCommandValidator
    : AbstractValidator<ReactivateProviderCommissionBenefitRuleCommand>
{
    public ReactivateProviderCommissionBenefitRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Rule Id must be a positive integer.");
    }
}
