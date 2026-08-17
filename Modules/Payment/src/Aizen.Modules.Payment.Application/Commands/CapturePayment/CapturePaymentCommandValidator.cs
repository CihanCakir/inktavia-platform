using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CapturePayment;

public sealed class CapturePaymentCommandValidator : AbstractValidator<CapturePaymentCommand>
{
    public CapturePaymentCommandValidator()
    {
        // TransactionId is optional — when null the handler resolves via GatewayReference
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).When(x => x.TransactionId.HasValue)
            .WithMessage("TransactionId must be greater than zero when provided.");

        RuleFor(x => x.GatewayReference)
            .NotEmpty().WithMessage("GatewayReference is required.");

        RuleFor(x => x.PaidAmount)
            .GreaterThan(0).WithMessage("PaidAmount must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be exactly 3 characters.");
    }
}
