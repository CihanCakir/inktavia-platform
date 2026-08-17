using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProfitProtectionPolicy;

public sealed class ReactivateProfitProtectionPolicyCommandValidator
    : AbstractValidator<ReactivateProfitProtectionPolicyCommand>
{
    public ReactivateProfitProtectionPolicyCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Policy Id must be a positive integer.");
    }
}
