using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCommissionRule;

public sealed class ReactivateCommissionRuleCommandValidator
    : AbstractValidator<ReactivateCommissionRuleCommand>
{
    public ReactivateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Rule Id must be a positive integer.");
    }
}
