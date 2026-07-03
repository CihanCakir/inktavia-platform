using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateConsignmentAgreement;

[DocumentationInfo("Create consignment agreement command handler",
    "Creates a new CargoDry consignment agreement in Draft status. " +
    "Validates uniqueness of AgreementCode. " +
    "Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class CreateConsignmentAgreementCommandHandler
    : AizenCommandHandler<CreateConsignmentAgreementCommand, CargoDryConsignmentAgreementDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public CreateConsignmentAgreementCommandHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementDto> Handle(
        CreateConsignmentAgreementCommand request, CancellationToken ct)
    {
        var existing = await _agreements.GetByAgreementCodeAsync(request.AgreementCode, ct);
        if (existing is not null)
            throw new InvalidOperationException(
                $"A consignment agreement with code '{request.AgreementCode}' already exists.");

        var hasActive = await _agreements.ExistsActiveForProviderProductAsync(
            request.ProviderProfileId, request.ProductCode, excludeId: null, ct);
        if (hasActive)
            throw new InvalidOperationException(
                $"An active consignment agreement already exists for provider {request.ProviderProfileId} " +
                $"and product '{request.ProductCode}'. Only one active agreement per provider+product is allowed.");

        var entity = CargoDryConsignmentAgreementEntity.Create(
            agreementCode:          request.AgreementCode,
            providerProfileId:      request.ProviderProfileId,
            productCode:            request.ProductCode,
            consignmentRate:        request.ConsignmentRate,
            minimumSettlementAmount: request.MinimumSettlementAmount,
            currencyCode:           request.CurrencyCode,
            maxKitCount:            request.MaxKitCount,
            startDateUtc:           request.StartDateUtc,
            endDateUtc:             request.EndDateUtc,
            termsDocumentRef:       request.TermsDocumentRef,
            notes:                  request.Notes);

        await _agreements.AddAsync(entity, ct);

        return MapToDto(entity);
    }

    internal static CargoDryConsignmentAgreementDto MapToDto(CargoDryConsignmentAgreementEntity e)
        => new()
        {
            Id                      = e.Id,
            AgreementCode           = e.AgreementCode,
            ProviderProfileId       = e.ProviderProfileId,
            ProductCode             = e.ProductCode,
            ConsignmentRate         = e.ConsignmentRate,
            MinimumSettlementAmount = e.MinimumSettlementAmount,
            CurrencyCode            = e.CurrencyCode,
            MaxKitCount             = e.MaxKitCount,
            AllocatedKitCount       = e.AllocatedKitCount,
            RemainingKitCount       = e.RemainingKitCount,
            Status                  = e.Status,
            StatusName              = e.Status.ToString(),
            StartDateUtc            = e.StartDateUtc,
            EndDateUtc              = e.EndDateUtc,
            TermsDocumentRef        = e.TermsDocumentRef,
            Notes                   = e.Notes,
            ActivatedAtUtc          = e.ActivatedAtUtc,
            SuspendedAtUtc          = e.SuspendedAtUtc,
            TerminatedAtUtc         = e.TerminatedAtUtc,
            SuspendReason           = e.SuspendReason,
            TerminationReason       = e.TerminationReason,
            CreatedAt               = e.CreateDate?.ToString("O"),
            UpdatedAt               = e.ModifyDate?.ToString("O"),
        };
}
