using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCommissionRule;

public sealed class UpdateCommissionRuleCommandValidator
    : AbstractValidator<UpdateCommissionRuleCommand>
{
    public UpdateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");

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
    }
}
