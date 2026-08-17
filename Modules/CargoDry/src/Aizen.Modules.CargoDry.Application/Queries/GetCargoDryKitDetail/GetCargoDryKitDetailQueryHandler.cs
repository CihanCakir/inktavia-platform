using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitDetail;

[DocumentationInfo("Get CargoDry kit detail query handler",
    "Returns a single full-detail kit record for admin inspection. " +
    "Uses GetByIdAsync which already exists in the repository. " +
    "Phase 8B (July 2026): closes the missing GET /admin/kits/{id} contract gap.")]
public sealed class GetCargoDryKitDetailQueryHandler
    : AizenQueryHandler<GetCargoDryKitDetailQuery, GetCargoDryKitDetailResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryKitDetailQueryHandler(
        ICargoDryKitRepository     kits,
        ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<GetCargoDryKitDetailResponse> Handle(
        GetCargoDryKitDetailQuery request, CancellationToken ct)
    {
        var kit = await _kits.GetByIdAsync(request.KitId, ct);
        if (kit is null)
            return new GetCargoDryKitDetailResponse { Kit = null };

        var product     = await _products.GetByCodeAsync(kit.ProductCode, ct);
        var productName = product?.Name ?? kit.ProductCode;

        return new GetCargoDryKitDetailResponse
        {
            Kit = MapToDetail(kit, productName),
        };
    }

    internal static CargoDryKitDetailDto MapToDetail(
        Aizen.Modules.CargoDry.Domain.Entities.CargoDryKitEntity k,
        string productName)
        => new()
        {
            Id                   = k.Id,
            SerialNumber         = k.SerialNumber,
            KitCode              = k.KitCode,
            ProductCode          = k.ProductCode,
            ProductName          = productName,
            BatchCode            = k.BatchCode,
            Status               = k.Status,
            OwnerUserId          = k.OwnerUserId,
            OwnerDisplayName     = null, // enriched at BFF layer if needed
            VesselId             = k.VesselId,
            VesselName           = null, // enriched at BFF layer if needed
            ActivatedAt          = k.ActivatedAt,
            ExpiresAt            = k.ExpiresAt,
            EfficiencyPercent    = k.EfficiencyPercent,
            DaysUntilExpiry      = k.DaysUntilExpiry,
            RenewalCount         = k.RenewalCount,
            ManufacturedAt       = k.ManufacturedAt,
            // Phase 0 commercial fields
            ProviderProfileId    = k.ProviderProfileId,
            SalesChannel         = k.SalesChannel,
            CommercialModel      = k.CommercialModel,
            StockLocationType    = k.StockLocationType,
            InvoiceId            = k.InvoiceId,
            PaymentTransactionId = k.PaymentTransactionId,
            WarehouseId          = k.WarehouseId,
            // Phase 8B admin-only fields
            ConsignmentAgreementId = k.ConsignmentAgreementId,
            QrPayload              = k.QrPayload,
            RevokeReason           = k.RevokeReason,
            RevokedAt              = k.RevokedAt,
            CreatedAtUtc           = k.CreateDate ?? DateTime.MinValue,
            UpdatedAtUtc           = k.ModifyDate,
        };
}
