using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
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

        // Fetch all kits for this product to compute operational stats.
        // GetAllAsync is cached at repository level (via distributed cache in other handlers);
        // for product detail this is an admin-only low-frequency call — acceptable.
        var allKits   = await _kits.GetAllAsync(ct);
        var kitsByPrd = allKits.Where(k => k.ProductCode == p.ProductCode).ToList();

        var total     = kitsByPrd.Count;
        var active    = kitsByPrd.Count(k => k.Status == CargoDryKitStatus.Activated);
        var expired   = kitsByPrd.Count(k => k.Status == CargoDryKitStatus.Expired);
        var revoked   = kitsByPrd.Count(k => k.Status == CargoDryKitStatus.Revoked);
        var renewed   = kitsByPrd.Count(k => k.RenewalCount > 0);
        var activated = kitsByPrd.Count(k => k.Status != CargoDryKitStatus.Available);
        var expIn30   = kitsByPrd.Count(k => k.IsExpiringSoon(30));
        var now       = DateTimeOffset.UtcNow;

        var activeKitsList = kitsByPrd.Where(k => k.Status == CargoDryKitStatus.Activated).ToList();
        double avgEff = activeKitsList.Count > 0
            ? activeKitsList.Average(k => k.EfficiencyPercent)
            : 0d;

        double renewalRate = activated > 0
            ? Math.Round(renewed / (double)activated * 100d, 1)
            : 0d;

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
                TotalKitsIssued  = total,
                ActiveKits       = active,
                ExpiredKits      = expired,
                RevokedKits      = revoked,
                RenewedKits      = renewed,
                AvgEfficiencyPct = Math.Round(avgEff, 1),
                RenewalRatePct   = renewalRate,
                ExpiringIn30Days = expIn30,
            },
        };
    }
}
