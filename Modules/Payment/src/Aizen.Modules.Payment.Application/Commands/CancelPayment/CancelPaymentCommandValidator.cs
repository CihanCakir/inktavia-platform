using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CancelPayment;

public sealed class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("TransactionId must be a valid positive identifier.");

        RuleFor(x => x.CancellationReason)
            .IsInEnum().WithMessage("A valid cancellation reason is required.");
    }
}
