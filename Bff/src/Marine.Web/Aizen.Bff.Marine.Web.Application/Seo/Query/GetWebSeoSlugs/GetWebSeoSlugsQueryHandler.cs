using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Seo;
using Aizen.Bff.Marine.Web.Application.Content;
using Aizen.Bff.Marine.Web.Application.Contracts.Seo;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Seo.Query.GetWebSeoSlugs;

public sealed class GetWebSeoSlugsQueryHandler
    : AizenQueryHandler<GetWebSeoSlugsQuery, List<WebSeoSlugItemDto>>
{
    // The module feed caps pageSize at 100; cap total pages so a runaway can never loop unbounded.
    private const int PageSize = 100;
    private const int MaxPages = 100;

    private const string ContentEntityType = "content";

    private readonly IContentRemoteCall _content;
    private readonly ISeoIndexabilityPolicy _seo;
    private readonly ILogger<GetWebSeoSlugsQueryHandler> _logger;

    public GetWebSeoSlugsQueryHandler(
        IContentRemoteCall content, ISeoIndexabilityPolicy seo, ILogger<GetWebSeoSlugsQueryHandler> logger)
    {
        _content = content;
        _seo = seo;
        _logger = logger;
    }

    public override async Task<List<WebSeoSlugItemDto>?> Handle(
        GetWebSeoSlugsQuery request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.EntityType, ContentEntityType, StringComparison.OrdinalIgnoreCase))
            throw new AizenBusinessException($"Unsupported SEO entity type '{request.EntityType}'. Only 'content' is available.");

        var items = new List<WebSeoSlugItemDto>();
        try
        {
            // Enumerate the whole published, MarineOsWeb-scoped feed (the slug feed must be complete for a sitemap).
            for (var page = 1; page <= MaxPages; page++)
            {
                var feed = await _content.GetPublicFeed(WebContentSurface.Pinned, request.Lang, page, PageSize);
                var rows = feed?.Items ?? new();
                foreach (var s in rows)
                {
                    // Summary-level verdict: no body here → the policy evaluates from title/description only.
                    var verdict = _seo.Evaluate(new SeoEvaluationInput(
                        IsPublished: true, Title: s.Title, Description: s.Summary, BodyLength: null));
                    items.Add(new WebSeoSlugItemDto
                    {
                        Slug = s.Slug,
                        AvailableLangs = s.AvailableLangs,
                        LastModified = s.PublishedAt ?? s.PublishAt,
                        Indexable = verdict.Indexable,
                    });
                }

                if (rows.Count < PageSize)
                    break;
            }

            return items;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "SEO slug feed for '{EntityType}' failed (status {Status}).",
                request.EntityType, ex.StatusCode);
            throw new AizenBusinessException("The slug feed is currently unavailable.");
        }
    }
}
