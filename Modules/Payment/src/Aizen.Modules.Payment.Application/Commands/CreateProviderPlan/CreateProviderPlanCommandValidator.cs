using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateProviderPlan;

public sealed class CreateProviderPlanCommandValidator
    : AbstractValidator<CreateProviderPlanCommand>
{
    public CreateProviderPlanCommandValidator()
    {
        RuleFor(x => x.PlanCode)
            .NotEmpty().MaximumLength(50)
            .Matches(@"^[A-Z0-9_]+$")
            .WithMessage("PlanCode must contain only uppercase letters, digits, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.MonthlyPriceTRY)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MonthlyPriceTRY cannot be negative.");

        RuleFor(x => x.MaxActiveOffers)
            .GreaterThan(0)
            .When(x => x.MaxActiveOffers.HasValue)
            .WithMessage("MaxActiveOffers must be greater than zero when specified.");

        RuleFor(x => x.SortOrder)
            .GreaterThan(0)
            .WithMessage("SortOrder must be greater than zero.");
    }
}
