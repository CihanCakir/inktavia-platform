using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.IssueInvoice;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("InvoiceId must be a valid positive identifier.");
    }
}
