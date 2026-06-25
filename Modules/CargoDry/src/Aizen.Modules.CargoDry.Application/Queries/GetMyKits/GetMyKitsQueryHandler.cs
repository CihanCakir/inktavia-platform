using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetMyKits;

public sealed class GetMyKitsQueryHandler
    : AizenQueryHandler<GetMyKitsQuery, GetMyKitsResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetMyKitsQueryHandler(
        ICargoDryKitRepository kits, ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<GetMyKitsResponse> Handle(
        GetMyKitsQuery request, CancellationToken ct)
    {
        var kits        = await _kits.GetByOwnerAsync(request.UserId, ct);
        var allProducts = await _products.GetAllActiveAsync(ct);
        var productMap  = allProducts.ToDictionary(p => p.ProductCode);

        var items = kits.Select(k => new CargoDryKitDto
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

        return new GetMyKitsResponse
        {
            Items         = items,
            Total         = items.Count,
            ActiveCount   = items.Count(i => i.Status == CargoDryKitStatus.Activated),
            ExpiringCount = items.Count(i => i.Status == CargoDryKitStatus.Activated && i.DaysUntilExpiry <= 30),
        };
    }
}
