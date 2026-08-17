using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateParticipantPlan;

public sealed class CreateParticipantPlanCommandValidator
    : AbstractValidator<CreateParticipantPlanCommand>
{
    public CreateParticipantPlanCommandValidator()
    {
        RuleFor(x => x.PlanCode)
            .NotEmpty().MaximumLength(50)
            .Matches(@"^[A-Z0-9_]+$")
            .WithMessage("PlanCode must contain only uppercase letters, digits, and underscores.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.MonthlyPriceTRY).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ServiceDiscountRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.CargoDryDiscountRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.InkCoinEarnMultiplier).GreaterThan(0m);
        RuleFor(x => x.SortOrder).GreaterThan(0);
    }
}
