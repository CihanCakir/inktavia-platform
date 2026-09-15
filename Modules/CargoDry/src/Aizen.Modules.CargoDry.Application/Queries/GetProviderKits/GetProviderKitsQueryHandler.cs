using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderKits;

[DocumentationInfo("Get provider kits query handler",
    "Returns the calling provider's own CargoDry kits (optionally filtered by product + status), newest first, " +
    "capped by PageSize. Product names are batch-resolved from the catalog. CargoDry supply v2 — provider kit picker.")]
public sealed class GetProviderKitsQueryHandler
    : AizenQueryHandler<GetProviderKitsQuery, List<CargoDryProviderKitDto>>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public GetProviderKitsQueryHandler(
        ICargoDryKitRepository     kits,
        ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<List<CargoDryProviderKitDto>> Handle(
        GetProviderKitsQuery request, CancellationToken ct)
    {
        // Clamp the cap defensively — the picker never needs an unbounded page.
        var take = request.PageSize is > 0 and <= 500 ? request.PageSize : 100;

        var kits = await _kits.GetProviderKitsAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.Status,
            take,
            ct);

        // Batch-load product names for the page's distinct product codes (mirrors GetProviderInventoryList).
        var productsByCode = new Dictionary<string, CargoDryProductEntity>();
        foreach (var code in kits.Select(k => k.ProductCode).Distinct())
        {
            var product = await _products.GetByCodeAsync(code, ct);
            if (product is not null)
                productsByCode[code] = product;
        }

        return kits.Select(k =>
        {
            productsByCode.TryGetValue(k.ProductCode, out var product);
            return new CargoDryProviderKitDto
            {
                KitId        = k.Id,
                KitCode      = k.KitCode,
                SerialNumber = k.SerialNumber,
                Status       = k.Status,
                StatusName   = k.Status.ToString(),
                ProductCode  = k.ProductCode,
                ProductName  = product?.Name,
            };
        }).ToList();
    }
}
