using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;

public sealed class GetCargoDryProductListQueryHandler
    : AizenQueryHandler<GetCargoDryProductListQuery, List<CargoDryProductDto>>
{
    // v2: payload now carries media ids (ThumbnailFileId / ImageFileIds); bumped so stale media-less entries are dropped.
    private const string CacheKey = "cargodry:products:all:v2";
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

        var products = await _products.GetAllWithImagesAsync(ct);
        var result   = products.Select(p => new CargoDryProductDto
        {
            Id                     = p.Id,
            ProductCode            = p.ProductCode,
            Name                   = p.Name,
            Description            = p.Description,
            ValidityDays           = p.ValidityDays,
            HasSmartDevice         = p.HasSmartDevice,
            DeviceType             = p.DeviceType,
            RetailPrice            = p.RetailPrice,
            CurrencyCode           = p.CurrencyCode,
            IsActive               = p.IsActive,
            CreatedAt              = p.CreateDate?.ToString("O"),
            // Phase 0 commercial pricing
            WholesalePrice         = p.WholesalePrice,
            ConsignmentPrice       = p.ConsignmentPrice,
            ProviderCommissionRate = p.ProviderCommissionRate,
            // Media (BFF resolves file ids → presigned URLs)
            ThumbnailFileId        = p.ThumbnailFileId,
            ImageFileIds           = p.Images.OrderBy(i => i.SortOrder).Select(i => i.FileId).ToList(),
        }).ToList();

        await _cache.SetAsync(result, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return result;
    }
}
