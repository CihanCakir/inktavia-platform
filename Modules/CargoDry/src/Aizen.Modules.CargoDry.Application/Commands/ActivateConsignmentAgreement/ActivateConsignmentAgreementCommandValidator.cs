using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateConsignmentAgreement;

public sealed class ActivateConsignmentAgreementCommandValidator
    : AbstractValidator<ActivateConsignmentAgreementCommand>
{
    public ActivateConsignmentAgreementCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
