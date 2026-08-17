using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.SuspendConsignmentAgreement;

public sealed class SuspendConsignmentAgreementCommandValidator
    : AbstractValidator<SuspendConsignmentAgreementCommand>
{
    public SuspendConsignmentAgreementCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
