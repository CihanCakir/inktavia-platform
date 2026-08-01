using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCustomerDiscountRule;

public sealed class ReactivateCustomerDiscountRuleCommandValidator
    : AbstractValidator<ReactivateCustomerDiscountRuleCommand>
{
    public ReactivateCustomerDiscountRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Rule Id must be a positive integer.");
    }
}
