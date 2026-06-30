using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateInvoiceDraft;

public sealed class CreateInvoiceDraftCommandValidator : AbstractValidator<CreateInvoiceDraftCommand>
{
    public CreateInvoiceDraftCommandValidator()
    {
        RuleFor(x => x.InvoiceType)
            .IsInEnum().WithMessage("A valid InvoiceType is required.");

        RuleFor(x => x.CommercialModel)
            .IsInEnum().WithMessage("A valid CommercialModel is required.");

        RuleFor(x => x.SourceType)
            .IsInEnum().WithMessage("A valid SourceType is required.");

        RuleFor(x => x.SellerName)
            .NotEmpty().WithMessage("SellerName is required.")
            .MaximumLength(200).WithMessage("SellerName must not exceed 200 characters.");

        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("BuyerName is required.")
            .MaximumLength(200).WithMessage("BuyerName must not exceed 200 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-character ISO 4217 code (e.g. TRY, USD).");

        RuleFor(x => x.Lines)
            .NotNull().WithMessage("At least one line item is required.")
            .Must(l => l is { Count: > 0 })
            .WithMessage("At least one line item is required.");

        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineDraftItemValidator());
    }
}

internal sealed class InvoiceLineDraftItemValidator : AbstractValidator<InvoiceLineDraftItem>
{
    public InvoiceLineDraftItemValidator()
    {
        RuleFor(x => x.LineNumber)
            .GreaterThan(0).WithMessage("LineNumber must be greater than 0.");

        RuleFor(x => x.LineType)
            .IsInEnum().WithMessage("A valid LineType is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Line description is required.")
            .MaximumLength(500).WithMessage("Line description must not exceed 500 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be zero or greater.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0m, 1m)
            .WithMessage("TaxRate must be between 0.0 and 1.0 (e.g. 0.20 for 20%).");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be zero or greater.");

        RuleFor(x => x.UnitCode)
            .NotEmpty().WithMessage("UnitCode is required.");
    }
}
