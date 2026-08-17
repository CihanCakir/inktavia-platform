using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentBySlug;

public sealed class GetPublicContentBySlugQueryHandler
    : AizenQueryHandler<GetPublicContentBySlugQuery, ContentItemDto?>
{
    private readonly IContentItemRepository _items;
    private readonly IAizenDistributedCache _cache;

    public GetPublicContentBySlugQueryHandler(IContentItemRepository items, IAizenDistributedCache cache)
    {
        _items = items;
        _cache = cache;
    }

    public override async Task<ContentItemDto?> Handle(
        GetPublicContentBySlugQuery request, CancellationToken cancellationToken)
    {
        var lang = string.IsNullOrWhiteSpace(request.Lang) ? "tr" : request.Lang;
        var globalGen = await ContentPublicCacheKeys.ReadGenerationAsync(
            _cache, ContentCacheInvalidator.GlobalGenerationKey, cancellationToken);
        var cacheKey = ContentPublicCacheKeys.BySlug(request.Slug, lang, globalGen);

        var (hit, cached) = await _cache.TryGetAsync<ContentItemDto>(cacheKey, cancellationToken);
        if (hit) return cached;

        var item = await _items.GetBySlugAsync(request.Slug, cancellationToken);
        if (item is null || !IsPubliclyVisible(item, DateTimeOffset.UtcNow))
            return null;

        var dto = ContentMapper.ToDto(item);
        await _cache.SetAsync(dto, cacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = ContentPublicCacheKeys.DetailTtl }, cancellationToken);

        return dto;
    }

    private static bool IsPubliclyVisible(ContentItemDocument item, DateTimeOffset now)
        => item.Status == ContentStatus.Published
           && item.Audience.Type == ContentAudienceType.Public
           && item.PublishAt is not null && item.PublishAt <= now
           && (item.ExpireAt is null || now < item.ExpireAt);
}
