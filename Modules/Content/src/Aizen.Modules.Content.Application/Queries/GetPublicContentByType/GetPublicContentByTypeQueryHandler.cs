using System.Linq.Expressions;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentByType;

public sealed class GetPublicContentByTypeQueryHandler
    : AizenQueryHandler<GetPublicContentByTypeQuery, ContentFeedResponse>
{
    // KNOWN LIMIT (backlog, see README): bounded candidate fetch + in-memory order/page, same as
    // GetPublicContentFeedQueryHandler (feed ordering is a per-item derived key). Aggregation-pipeline rewrite pending.
    private const int CandidateCap = 500;

    private readonly IContentItemRepository _items;
    private readonly IAizenDistributedCache _cache;

    public GetPublicContentByTypeQueryHandler(IContentItemRepository items, IAizenDistributedCache cache)
    {
        _items = items;
        _cache = cache;
    }

    public override async Task<ContentFeedResponse> Handle(
        GetPublicContentByTypeQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var lang = string.IsNullOrWhiteSpace(request.Lang) ? "tr" : request.Lang;
        var surface = request.Surface;
        var type = request.Type;

        var (globalGen, surfaceGen) = await ContentPublicCacheKeys.ReadGenerationsAsync(_cache, surface, cancellationToken);
        var cacheKey = ContentPublicCacheKeys.ByType(surface, type, lang, page, pageSize, globalGen, surfaceGen);

        var (hit, cached) = await _cache.TryGetAsync<ContentFeedResponse>(cacheKey, cancellationToken);
        if (hit) return cached;

        Expression<Func<ContentItemDocument, bool>> predicate = x =>
            x.Status == ContentStatus.Published &&
            x.Type == type &&
            x.Audience.Type == ContentAudienceType.Public &&
            x.Placements.Any(p => p.Surface == surface);

        var candidates = await _items.FindManyAsync(predicate, skip: 0, take: CandidateCap, cancellationToken);

        var response = ContentFeedProjection.Build(candidates, surface, lang, page, pageSize, DateTimeOffset.UtcNow);

        await _cache.SetAsync(response, cacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = ContentPublicCacheKeys.FeedTtl }, cancellationToken);

        return response;
    }
}
