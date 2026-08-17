using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.TerminateConsignmentAgreement;

public sealed class TerminateConsignmentAgreementCommand : AizenCommand<CargoDryConsignmentAgreementDto>
{
    public long   Id     { get; init; }
    public string Reason { get; init; } = default!;
}
