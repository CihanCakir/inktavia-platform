using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDrySettlementPayout;

public sealed class CompleteCargoDrySettlementPayoutCommandValidator
    : AbstractValidator<CompleteCargoDrySettlementPayoutCommand>
{
    public CompleteCargoDrySettlementPayoutCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be greater than zero.");

        RuleFor(x => x.CompletedByUserId)
            .GreaterThan(0).WithMessage("CompletedByUserId must be greater than zero.");

        RuleFor(x => x.ManualPaymentReference)
            .NotEmpty().WithMessage("ManualPaymentReference is required. Provide the bank transfer reference or payment confirmation identifier.")
            .MaximumLength(500).WithMessage("ManualPaymentReference must not exceed 500 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 1000 characters.");
    }
}
