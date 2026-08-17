using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ApproveCargoDrySettlementPayout;

public sealed class ApproveCargoDrySettlementPayoutCommandValidator
    : AbstractValidator<ApproveCargoDrySettlementPayoutCommand>
{
    public ApproveCargoDrySettlementPayoutCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be greater than zero.");

        RuleFor(x => x.ApprovedByUserId)
            .GreaterThan(0).WithMessage("ApprovedByUserId must be greater than zero.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 1000 characters.");
    }
}
