using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQueryHandler
    : AizenQueryHandler<GetMyKitsQuery, List<CargoDryKitDto>>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetMyKitsQueryHandler(ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<List<CargoDryKitDto>> Handle(GetMyKitsQuery request, CancellationToken ct)
    {
        var kits       = await _kits.GetByOwnerAsync(request.UserId, ct);
        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        return kits.Select(k => new CargoDryKitDto
        {
            Id                = k.Id,
            SerialNumber      = k.SerialNumber,
            KitCode           = k.KitCode,
            ProductCode       = k.ProductCode,
            ProductName       = productMap.TryGetValue(k.ProductCode, out var p) ? p.Name : k.ProductCode,
            BatchCode         = k.BatchCode,
            Status            = k.Status,
            OwnerUserId       = k.OwnerUserId,
            VesselId          = k.VesselId,
            ActivatedAt       = k.ActivatedAt,
            ExpiresAt         = k.ExpiresAt,
            EfficiencyPercent = k.EfficiencyPercent,
            DaysUntilExpiry   = k.DaysUntilExpiry,
            RenewalCount      = k.RenewalCount,
            ManufacturedAt    = k.ManufacturedAt,
        }).ToList();
    }
}
