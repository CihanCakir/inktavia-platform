using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;

public sealed class GetAdminKitListQueryHandler
    : AizenQueryHandler<GetAdminKitListQuery, GetAdminKitListResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetAdminKitListQueryHandler(ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<GetAdminKitListResponse> Handle(
        GetAdminKitListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _kits.GetPagedAsync(
            request.Status, request.Search, request.VesselId, request.OwnerUserId, request.BatchCode, skip, request.PageSize, ct: ct);

        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        return new GetAdminKitListResponse
        {
            Items = items.Select(k => new CargoDryKitDto
            {
                Id                   = k.Id,
                SerialNumber         = k.SerialNumber,
                KitCode              = k.KitCode,
                ProductCode          = k.ProductCode,
                ProductName          = productMap.TryGetValue(k.ProductCode, out var p) ? p.Name : k.ProductCode,
                BatchCode            = k.BatchCode,
                Status               = k.Status,
                OwnerUserId          = k.OwnerUserId,
                VesselId             = k.VesselId,
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
                // Phase 0 addendum
                WarehouseId          = k.WarehouseId,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
