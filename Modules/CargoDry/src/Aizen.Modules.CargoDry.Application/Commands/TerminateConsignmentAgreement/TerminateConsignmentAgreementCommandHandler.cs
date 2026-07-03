using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Commands.TerminateConsignmentAgreement;

[DocumentationInfo("Terminate consignment agreement command handler",
    "Terminates an Active or Suspended agreement. Terminal state — no further transitions. " +
    "Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class TerminateConsignmentAgreementCommandHandler
    : AizenCommandHandler<TerminateConsignmentAgreementCommand, CargoDryConsignmentAgreementDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public TerminateConsignmentAgreementCommandHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto> Handle(
        TerminateConsignmentAgreementCommand request, CancellationToken ct)
    {
        var entity = await _agreements.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException(
                $"Consignment agreement with id {request.Id} not found.");

        entity.Terminate(request.Reason);

        await _agreements.SaveChangesAsync(ct);

        return MapToDto(entity);
    }
}
