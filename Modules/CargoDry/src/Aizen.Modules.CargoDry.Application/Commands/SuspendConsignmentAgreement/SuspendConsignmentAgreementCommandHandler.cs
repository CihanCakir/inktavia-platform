using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Commands.SuspendConsignmentAgreement;

[DocumentationInfo("Suspend consignment agreement command handler",
    "Suspends an Active agreement. Allocation pauses; existing kits remain with the provider. " +
    "Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class SuspendConsignmentAgreementCommandHandler
    : AizenCommandHandler<SuspendConsignmentAgreementCommand, CargoDryConsignmentAgreementDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public SuspendConsignmentAgreementCommandHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto> Handle(
        SuspendConsignmentAgreementCommand request, CancellationToken ct)
    {
        var entity = await _agreements.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException(
                $"Consignment agreement with id {request.Id} not found.");

        entity.Suspend(request.Reason);

        await _agreements.SaveChangesAsync(ct);

        return MapToDto(entity);
    }
}
