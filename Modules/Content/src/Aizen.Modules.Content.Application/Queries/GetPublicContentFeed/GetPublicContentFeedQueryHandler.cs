using System.Linq.Expressions;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Content.Application.Queries.GetPublicContentFeed;

public sealed class GetPublicContentFeedQueryHandler
    : AizenQueryHandler<GetPublicContentFeedQuery, ContentFeedResponse>
{
    // KNOWN LIMIT (backlog, see README "Known limits"): feed ordering keys off the surface-matched
    // placement position (a per-item derived value not expressible as a server-side sort), so we fetch a
    // bounded candidate set and order/page in memory. A Mongo aggregation pipeline (precomputed sort key)
    // should replace this when a surface's published set can exceed CandidateCap. Truncation is logged.
    private const int CandidateCap = 500;

    private readonly IContentItemRepository _items;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<GetPublicContentFeedQueryHandler> _logger;

    public GetPublicContentFeedQueryHandler(
        IContentItemRepository items, IAizenDistributedCache cache, ILogger<GetPublicContentFeedQueryHandler> logger)
    {
        _items = items;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<ContentFeedResponse> Handle(
        GetPublicContentFeedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var lang = string.IsNullOrWhiteSpace(request.Lang) ? "tr" : request.Lang;
        var surface = request.Surface;

        var (globalGen, surfaceGen) = await ContentPublicCacheKeys.ReadGenerationsAsync(_cache, surface, cancellationToken);
        var cacheKey = ContentPublicCacheKeys.Feed(surface, lang, page, pageSize, globalGen, surfaceGen);

        var (hit, cached) = await _cache.TryGetAsync<ContentFeedResponse>(cacheKey, cancellationToken);
        if (hit) return cached;

        // Anonymous public callers see Public-audience items only. Providers/VesselOwners/Segment
        // audiences belong to the authenticated provider/participant surfaces (out of C5 scope).
        Expression<Func<ContentItemDocument, bool>> predicate = x =>
            x.Status == ContentStatus.Published &&
            x.Audience.Type == ContentAudienceType.Public &&
            x.Placements.Any(p => p.Surface == surface);

        var candidates = await _items.FindManyAsync(predicate, skip: 0, take: CandidateCap, cancellationToken);
        if (candidates.Count >= CandidateCap)
            _logger.LogWarning(
                "Content feed for surface={Surface} hit the {Cap}-item candidate cap; results may be truncated. " +
                "Migrate to a server-side aggregation pipeline (see README known limits).", surface, CandidateCap);

        var response = ContentFeedProjection.Build(candidates, surface, lang, page, pageSize, DateTimeOffset.UtcNow);

        await _cache.SetAsync(response, cacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = ContentPublicCacheKeys.FeedTtl }, cancellationToken);

        return response;
    }
}
