using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CancelInvoice;

public sealed class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("InvoiceId must be a valid positive identifier.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason must not exceed 500 characters.")
            .When(x => x.Reason is not null);
    }
}
