using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;

public sealed class ReinstateCancelledTransactionCommandValidator
    : AbstractValidator<ReinstateCancelledTransactionCommand>
{
    public ReinstateCancelledTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("TransactionId must be a valid positive identifier.");

        RuleFor(x => x.AdminNote)
            .NotEmpty().WithMessage("Admin note is required when reinstating a cancelled transaction.")
            .MaximumLength(500);
    }
}
