using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;

namespace Aizen.Modules.Content.Application.Queries.GetPublicCategoryTree;

public sealed class GetPublicCategoryTreeQueryHandler
    : AizenQueryHandler<GetPublicCategoryTreeQuery, List<ContentCategoryDto>>
{
    private readonly IContentCategoryRepository _categories;
    private readonly IAizenDistributedCache _cache;

    public GetPublicCategoryTreeQueryHandler(IContentCategoryRepository categories, IAizenDistributedCache cache)
    {
        _categories = categories;
        _cache = cache;
    }

    public override async Task<List<ContentCategoryDto>> Handle(
        GetPublicCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var lang = string.IsNullOrWhiteSpace(request.Lang) ? "tr" : request.Lang;

        // Category create/update bump the GLOBAL generation (C9), so category edits invalidate this cache
        // immediately; the 30-minute TTL is only the idle backstop.
        var globalGen = await ContentPublicCacheKeys.ReadGenerationAsync(
            _cache, ContentCacheInvalidator.GlobalGenerationKey, cancellationToken);
        var cacheKey = ContentPublicCacheKeys.CategoryTree(lang, globalGen);

        var (hit, cached) = await _cache.TryGetAsync<List<ContentCategoryDto>>(cacheKey, cancellationToken);
        if (hit) return cached;

        var categories = await _categories.GetAllAsync(activeOnly: true, cancellationToken);
        var result = categories
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Slug)
            .Select(ContentMapper.ToDto)
            .ToList();

        await _cache.SetAsync(result, cacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = ContentPublicCacheKeys.CategoryTtl }, cancellationToken);

        return result;
    }
}
