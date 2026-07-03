using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateConsignmentAgreement;

[DocumentationInfo("Activate consignment agreement command handler",
    "Transitions a Draft agreement to Active. Validates no other Active agreement exists " +
    "for the same provider+product pair. Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class ActivateConsignmentAgreementCommandHandler
    : AizenCommandHandler<ActivateConsignmentAgreementCommand, CargoDryConsignmentAgreementDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public ActivateConsignmentAgreementCommandHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto> Handle(
        ActivateConsignmentAgreementCommand request, CancellationToken ct)
    {
        var entity = await _agreements.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException(
                $"Consignment agreement with id {request.Id} not found.");

        // Guard: no other active agreement for the same provider+product
        var conflict = await _agreements.ExistsActiveForProviderProductAsync(
            entity.ProviderProfileId, entity.ProductCode, excludeId: entity.Id, ct);
        if (conflict)
            throw new InvalidOperationException(
                $"Another active consignment agreement already exists for provider " +
                $"{entity.ProviderProfileId} and product '{entity.ProductCode}'.");

        entity.Activate();

        await _agreements.SaveChangesAsync(ct);

        return MapToDto(entity);
    }
}
