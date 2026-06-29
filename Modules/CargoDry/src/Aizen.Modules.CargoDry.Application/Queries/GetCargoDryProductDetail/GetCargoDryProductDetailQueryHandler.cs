using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductDetail;

public sealed class GetCargoDryProductDetailQueryHandler
    : AizenQueryHandler<GetCargoDryProductDetailQuery, CargoDryProductDto?>
{
    private readonly ICargoDryProductRepository _products;
    private readonly ICargoDryKitRepository     _kits;

    public GetCargoDryProductDetailQueryHandler(
        ICargoDryProductRepository products,
        ICargoDryKitRepository     kits)
    {
        _products = products;
        _kits     = kits;
    }

    public override async Task<CargoDryProductDto?> Handle(
        GetCargoDryProductDetailQuery request, CancellationToken ct)
    {
        var p = await _products.GetByCodeAsync(request.ProductCode, ct);
        if (p is null) return null;

        // Use SQL-level aggregation instead of loading all kits into memory.
        var s = await _kits.GetKitStatsByProductCodeAsync(p.ProductCode, ct);

        return new CargoDryProductDto
        {
            Id             = p.Id,
            ProductCode    = p.ProductCode,
            Name           = p.Name,
            Description    = p.Description,
            ValidityDays   = p.ValidityDays,
            HasSmartDevice = p.HasSmartDevice,
            DeviceType     = p.DeviceType,
            RetailPrice    = p.RetailPrice,
            CurrencyCode   = p.CurrencyCode,
            IsActive       = p.IsActive,
            CreatedAt      = p.CreateDate?.ToString("O"),
            KitStats       = new CargoDryProductKitStatsDto
            {
                TotalKitsIssued  = s.TotalKits,
                ActiveKits       = s.ActiveKits,
                ExpiredKits      = s.ExpiredKits,
                RevokedKits      = s.RevokedKits,
                RenewedKits      = s.RenewedKits,
                AvgEfficiencyPct = s.AvgEfficiencyPercent,
                RenewalRatePct   = s.RenewalRatePercent,
                ExpiringIn30Days = s.ExpiringIn30Days,
            },
        };
    }
}
