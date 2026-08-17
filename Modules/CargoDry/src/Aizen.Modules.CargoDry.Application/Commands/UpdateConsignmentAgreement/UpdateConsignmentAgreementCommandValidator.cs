using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.UpdateConsignmentAgreement;

public sealed class UpdateConsignmentAgreementCommandValidator
    : AbstractValidator<UpdateConsignmentAgreementCommand>
{
    public UpdateConsignmentAgreementCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.ConsignmentRate)
            .InclusiveBetween(0.01m, 1.00m)
            .WithMessage("ConsignmentRate must be between 0.01 and 1.00.");

        RuleFor(x => x.MinimumSettlementAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3);

        RuleFor(x => x.MaxKitCount)
            .GreaterThan(0);

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
