using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementPayment;

public sealed class PrepareCargoDrySettlementPaymentCommandValidator
    : AbstractValidator<PrepareCargoDrySettlementPaymentCommand>
{
    public PrepareCargoDrySettlementPaymentCommandValidator()
    {
        RuleFor(x => x.SettlementId)
            .GreaterThan(0).WithMessage("SettlementId must be greater than zero.");

        RuleFor(x => x.PreparedByUserId)
            .GreaterThan(0).WithMessage("PreparedByUserId must be greater than zero.");

        RuleFor(x => x.PreparationNote)
            .MaximumLength(1000).When(x => x.PreparationNote is not null)
            .WithMessage("PreparationNote must not exceed 1000 characters.");
    }
}
