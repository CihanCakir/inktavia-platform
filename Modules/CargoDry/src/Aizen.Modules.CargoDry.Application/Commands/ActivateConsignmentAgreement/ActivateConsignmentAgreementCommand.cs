using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateConsignmentAgreement;

public sealed class ActivateConsignmentAgreementCommand : AizenCommand<CargoDryConsignmentAgreementDto>
{
    public long Id { get; init; }
}
