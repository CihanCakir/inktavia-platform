using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderProductPerformance;

public sealed class GetCargoDryProviderProductPerformanceQueryHandler
    : AizenQueryHandler<GetCargoDryProviderProductPerformanceQuery, List<CargoDryProductPerformanceDto>>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryProviderProductPerformanceQueryHandler(
        ICargoDrySalesAttributionRepository attributions,
        ICargoDryProductRepository products)
    {
        _attributions = attributions;
        _products     = products;
    }

    public override async Task<List<CargoDryProductPerformanceDto>?> Handle(
        GetCargoDryProviderProductPerformanceQuery request, CancellationToken ct)
    {
        var byProductBatch = await _attributions.SumProviderCommissionByProductBatchAsync(
            request.ProviderProfileId, ct);

        var allProducts = await _products.GetAllActiveAsync(ct);
        var productCurrencies = allProducts.ToDictionary(p => p.ProductCode, p => p.CurrencyCode);

        var byProduct = byProductBatch
            .GroupBy(kv => kv.Key.ProductCode)
            .Select(g =>
            {
                productCurrencies.TryGetValue(g.Key, out var currency);
                return new CargoDryProductPerformanceDto
                {
                    ProductCode      = g.Key,
                    EarnedCommission = g.Sum(kv => kv.Value),
                    CurrencyCode     = currency ?? "TRY",
                };
            })
            .OrderByDescending(p => p.EarnedCommission)
            .ToList();

        return byProduct;
    }
}
