using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;

public sealed class ResolveMonthlySellThroughSettlementCommandValidator
    : AbstractValidator<ResolveMonthlySellThroughSettlementCommand>
{
    public ResolveMonthlySellThroughSettlementCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be a valid entity Id.");

        RuleFor(x => x.ResolvedByUserId)
            .GreaterThan(0).WithMessage("ResolvedByUserId must be a valid user Id.");

        RuleFor(x => x.ResolutionNote)
            .MaximumLength(1000).WithMessage("ResolutionNote must not exceed 1000 characters.")
            .When(x => x.ResolutionNote is not null);
    }
}
