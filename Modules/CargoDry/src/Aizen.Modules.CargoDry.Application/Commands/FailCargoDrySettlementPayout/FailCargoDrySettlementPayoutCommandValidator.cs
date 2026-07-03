using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.FailCargoDrySettlementPayout;

public sealed class FailCargoDrySettlementPayoutCommandValidator
    : AbstractValidator<FailCargoDrySettlementPayoutCommand>
{
    public FailCargoDrySettlementPayoutCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be greater than zero.");

        RuleFor(x => x.FailedByUserId)
            .GreaterThan(0).WithMessage("FailedByUserId must be greater than zero.");

        RuleFor(x => x.FailureReason)
            .NotEmpty().WithMessage("FailureReason is required. Describe why the payout failed.")
            .MaximumLength(1000).WithMessage("FailureReason must not exceed 1000 characters.");

        RuleFor(x => x.ExternalReference)
            .MaximumLength(500).When(x => x.ExternalReference is not null)
            .WithMessage("ExternalReference must not exceed 500 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 1000 characters.");
    }
}
