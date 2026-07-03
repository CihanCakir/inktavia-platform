using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.TerminateConsignmentAgreement;

public sealed class TerminateConsignmentAgreementCommandValidator
    : AbstractValidator<TerminateConsignmentAgreementCommand>
{
    public TerminateConsignmentAgreementCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
