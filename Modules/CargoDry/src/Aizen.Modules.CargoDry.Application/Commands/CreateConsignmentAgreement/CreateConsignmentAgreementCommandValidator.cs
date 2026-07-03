using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement;

public sealed class CreateConsignmentAgreementCommandValidator
    : AbstractValidator<CreateConsignmentAgreementCommand>
{
    public CreateConsignmentAgreementCommandValidator()
    {
        RuleFor(x => x.AgreementCode)
            .NotEmpty().MaximumLength(60)
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("AgreementCode must be uppercase alphanumeric (A-Z, 0-9, _, -).");

        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0);

        RuleFor(x => x.ProductCode)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.ConsignmentRate)
            .InclusiveBetween(0.01m, 1.00m)
            .WithMessage("ConsignmentRate must be between 0.01 and 1.00 (i.e. 1% to 100%).");

        RuleFor(x => x.MinimumSettlementAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3)
            .WithMessage("CurrencyCode must be a 3-letter ISO code.");

        RuleFor(x => x.MaxKitCount)
            .GreaterThan(0)
            .WithMessage("MaxKitCount must be at least 1.");

        RuleFor(x => x.StartDateUtc)
            .NotEmpty();

        RuleFor(x => x.EndDateUtc)
            .GreaterThan(x => x.StartDateUtc)
            .When(x => x.EndDateUtc.HasValue)
            .WithMessage("EndDateUtc must be after StartDateUtc.");

        RuleFor(x => x.TermsDocumentRef)
            .MaximumLength(500)
            .When(x => x.TermsDocumentRef is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
