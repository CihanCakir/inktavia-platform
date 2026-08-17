using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryList;

[DocumentationInfo("Get provider inventory list query handler",
    "Returns paged provider inventory rows with optional filters. Phase 2 — CargoDry provider inventory.")]
public sealed class GetProviderInventoryListQueryHandler
    : AizenQueryHandler<GetProviderInventoryListQuery, CargoDryProviderInventoryPagedResultDto>
{
    private readonly ICargoDryProviderInventoryRepository _inventories;
    private readonly ICargoDryProductRepository           _products;
    private readonly ICargoDrySalesAttributionRepository  _attributions;

    public GetProviderInventoryListQueryHandler(
        ICargoDryProviderInventoryRepository inventories,
        ICargoDryProductRepository           products,
        ICargoDrySalesAttributionRepository  attributions)
    {
        _inventories  = inventories;
        _products     = products;
        _attributions = attributions;
    }

    public override async Task<CargoDryProviderInventoryPagedResultDto> Handle(
        GetProviderInventoryListQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _inventories.GetPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.CommercialModel,
            request.SalesChannel,
            request.HasAvailableStock,
            request.Search,
            skip,
            request.PageSize,
            ct);

        // Batch-load products for the page's distinct product codes
        var productCodes   = items.Select(i => i.ProductCode).Distinct().ToList();
        var productsByCode = new Dictionary<string, CargoDryProductEntity>();
        foreach (var code in productCodes)
        {
            var product = await _products.GetByCodeAsync(code, ct);
            if (product is not null)
                productsByCode[code] = product;
        }

        // Batch-load earned commission grouped by product+batch
        var earnedLookup = request.ProviderProfileId.HasValue
            ? await _attributions.SumProviderCommissionByProductBatchAsync(
                request.ProviderProfileId.Value, ct)
            : new Dictionary<(string, string?), decimal>();

        return new CargoDryProviderInventoryPagedResultDto
        {
            Items    = items.Select(e => MapToListItem(e, productsByCode, earnedLookup)).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }

    private static CargoDryProviderInventoryListItemDto MapToListItem(
        CargoDryProviderInventoryEntity e,
        Dictionary<string, CargoDryProductEntity> productsByCode,
        Dictionary<(string ProductCode, string? BatchCode), decimal> earnedLookup)
    {
        productsByCode.TryGetValue(e.ProductCode, out var product);
        var earningPerSale = product?.ProviderEarningPerSale() ?? 0m;

        earnedLookup.TryGetValue((e.ProductCode, e.BatchCode), out var earned);

        var potential = e.AvailableStock * earningPerSale;
        var sellThrough = e.TotalAllocated > 0
            ? Math.Round((decimal)e.TotalActivated * 100m / e.TotalAllocated, 1)
            : 0m;

        return new()
        {
            Id                  = e.Id,
            ProviderProfileId   = e.ProviderProfileId,
            ProductCode         = e.ProductCode,
            BatchCode           = e.BatchCode,
            CommercialModel     = e.CommercialModel,
            CommercialModelName = e.CommercialModel.ToString(),
            SalesChannel        = e.SalesChannel,
            SalesChannelName    = e.SalesChannel.ToString(),
            TotalAllocated      = e.TotalAllocated,
            TotalActivated      = e.TotalActivated,
            AvailableStock      = e.AvailableStock,
            EarnedCommission    = earned,
            PotentialCommission = potential,
            SellThroughPct      = sellThrough,
            CurrencyCode        = product?.CurrencyCode ?? "TRY",
            LastMovementAtUtc   = e.LastMovementAtUtc,
            CreatedAtUtc        = e.CreatedAtUtc,
        };
    }
}
