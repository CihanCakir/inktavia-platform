using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementPayoutPreparation;

public sealed class CreateCargoDrySettlementPayoutPreparationCommandValidator
    : AbstractValidator<CreateCargoDrySettlementPayoutPreparationCommand>
{
    public CreateCargoDrySettlementPayoutPreparationCommandValidator()
    {
        RuleFor(x => x.SourceSettlementId)
            .GreaterThan(0).WithMessage("SourceSettlementId must be greater than zero.");

        RuleFor(x => x.SettlementCode)
            .NotEmpty().WithMessage("SettlementCode is required.")
            .MaximumLength(100).WithMessage("SettlementCode must not exceed 100 characters.");

        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be greater than zero.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be a 3-character ISO 4217 code.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.PreparedByUserId)
            .GreaterThan(0).WithMessage("PreparedByUserId must be greater than zero.");
    }
}
