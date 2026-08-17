using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using static Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement.CreateConsignmentAgreementCommandHandler;

namespace Aizen.Modules.CargoDry.Application.Commands.UpdateConsignmentAgreement;

[DocumentationInfo("Update consignment agreement command handler",
    "Updates commercial terms of a Draft or Suspended agreement. " +
    "Active agreements cannot be updated — suspend first. " +
    "Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class UpdateConsignmentAgreementCommandHandler
    : AizenCommandHandler<UpdateConsignmentAgreementCommand, CargoDryConsignmentAgreementDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public UpdateConsignmentAgreementCommandHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto> Handle(
        UpdateConsignmentAgreementCommand request, CancellationToken ct)
    {
        var entity = await _agreements.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException(
                $"Consignment agreement with id {request.Id} not found.");

        entity.UpdateTerms(
            consignmentRate:        request.ConsignmentRate,
            minimumSettlementAmount: request.MinimumSettlementAmount,
            currencyCode:           request.CurrencyCode,
            maxKitCount:            request.MaxKitCount,
            startDateUtc:           request.StartDateUtc,
            endDateUtc:             request.EndDateUtc,
            termsDocumentRef:       request.TermsDocumentRef,
            notes:                  request.Notes);

        await _agreements.SaveChangesAsync(ct);

        return MapToDto(entity);
    }
}
