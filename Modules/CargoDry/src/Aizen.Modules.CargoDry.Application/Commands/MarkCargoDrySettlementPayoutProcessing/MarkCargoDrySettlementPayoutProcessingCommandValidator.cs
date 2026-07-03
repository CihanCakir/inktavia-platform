using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.MarkCargoDrySettlementPayoutProcessing;

public sealed class MarkCargoDrySettlementPayoutProcessingCommandValidator
    : AbstractValidator<MarkCargoDrySettlementPayoutProcessingCommand>
{
    public MarkCargoDrySettlementPayoutProcessingCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be greater than zero.");

        RuleFor(x => x.ProcessedByUserId)
            .GreaterThan(0).WithMessage("ProcessedByUserId must be greater than zero.");

        RuleFor(x => x.ExternalReference)
            .MaximumLength(500).When(x => x.ExternalReference is not null)
            .WithMessage("ExternalReference must not exceed 500 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => x.Note is not null)
            .WithMessage("Note must not exceed 1000 characters.");
    }
}
