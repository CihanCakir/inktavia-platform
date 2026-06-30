using Aizen.Modules.Payment.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;

public sealed class CreatePaymentEscrowCommandValidator : AbstractValidator<CreatePaymentEscrowCommand>
{
    public CreatePaymentEscrowCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.")
            .MaximumLength(100).WithMessage("IdempotencyKey must not exceed 100 characters.");

        RuleFor(x => x.Context)
            .NotNull().WithMessage("Transaction context is required.");

        RuleFor(x => x.TransactionType)
            .IsInEnum().WithMessage("TransactionType must be a valid enum value.");

        RuleFor(x => x.PayerProfileId)
            .GreaterThan(0).WithMessage("PayerProfileId must be greater than zero.");

        RuleFor(x => x.GrossAmount)
            .GreaterThan(0).WithMessage("GrossAmount must be greater than zero.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be zero or greater.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be exactly 3 characters.");
    }
}
