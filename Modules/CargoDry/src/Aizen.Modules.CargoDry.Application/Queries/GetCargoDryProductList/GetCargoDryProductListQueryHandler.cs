using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;

public sealed class GetCargoDryProductListQueryHandler
    : AizenQueryHandler<GetCargoDryProductListQuery, List<CargoDryProductDto>>
{
    private const string CacheKey = "cargodry:products:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly ICargoDryProductRepository _products;
    private readonly IAizenDistributedCache     _cache;

    public GetCargoDryProductListQueryHandler(
        ICargoDryProductRepository products,
        IAizenDistributedCache cache)
    {
        _products = products;
        _cache    = cache;
    }

    public override async Task<List<CargoDryProductDto>> Handle(
        GetCargoDryProductListQuery request, CancellationToken ct)
    {
        var (hit, cached) = await _cache.TryGetAsync<List<CargoDryProductDto>>(CacheKey, ct);
        if (hit) return cached;

        var products = await _products.GetAllActiveAsync(ct);
        var result   = products.Select(p => new CargoDryProductDto
        {
            Id             = p.Id,
            ProductCode    = p.ProductCode,
            Name           = p.Name,
            Description    = p.Description,
            ValidityDays   = p.ValidityDays,
            HasSmartDevice = p.HasSmartDevice,
            RetailPrice    = p.RetailPrice,
            CurrencyCode   = p.CurrencyCode,
            IsActive       = p.IsActive,
        }).ToList();

        await _cache.SetAsync(result, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return result;
    }
}
