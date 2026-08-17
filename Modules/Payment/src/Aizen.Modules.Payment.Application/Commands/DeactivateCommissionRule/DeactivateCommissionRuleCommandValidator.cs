using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateCommissionRule;

public sealed class DeactivateCommissionRuleCommandValidator
    : AbstractValidator<DeactivateCommissionRuleCommand>
{
    public DeactivateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");
    }
}
