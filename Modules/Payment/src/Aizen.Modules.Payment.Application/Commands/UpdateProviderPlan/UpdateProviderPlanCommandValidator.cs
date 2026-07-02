using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProviderPlan;

public sealed class UpdateProviderPlanCommandValidator
    : AbstractValidator<UpdateProviderPlanCommand>
{
    public UpdateProviderPlanCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.MonthlyPriceTRY).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxActiveOffers)
            .GreaterThan(0).When(x => x.MaxActiveOffers.HasValue)
            .WithMessage("MaxActiveOffers must be greater than zero when specified.");
        RuleFor(x => x.SortOrder).GreaterThan(0);
    }
}
