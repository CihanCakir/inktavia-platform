using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCommissionRule;

public sealed class UpdateCommissionRuleCommandValidator
    : AbstractValidator<UpdateCommissionRuleCommand>
{
    public UpdateCommissionRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than zero.");

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
    }
}
