using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;

public sealed class ReleasePaymentEscrowCommandValidator : AbstractValidator<ReleasePaymentEscrowCommand>
{
    public ReleasePaymentEscrowCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("TransactionId must be greater than zero.");

        RuleFor(x => x.ApprovedByUserId)
            .GreaterThan(0).WithMessage("ApprovedByUserId must be greater than zero.");
    }
}
