using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReactivatePlatformFeeRule;

public sealed class ReactivatePlatformFeeRuleCommandValidator
    : AbstractValidator<ReactivatePlatformFeeRuleCommand>
{
    public ReactivatePlatformFeeRuleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Rule Id must be a positive integer.");
    }
}
