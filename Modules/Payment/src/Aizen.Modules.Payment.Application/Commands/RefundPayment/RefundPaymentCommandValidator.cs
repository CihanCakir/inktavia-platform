using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.RefundPayment;

public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("TransactionId must be greater than zero.");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0).WithMessage("RefundAmount must be greater than zero.");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Reason must be a valid RefundReason enum value.");

        RuleFor(x => x.RefundType)
            .IsInEnum().WithMessage("RefundType must be a valid RefundType enum value.");
    }
}
