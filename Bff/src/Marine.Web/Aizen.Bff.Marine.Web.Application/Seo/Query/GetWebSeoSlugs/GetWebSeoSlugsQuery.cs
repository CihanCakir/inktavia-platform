using Aizen.Bff.Marine.Web.Application.Contracts.Seo;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Seo.Query.GetWebSeoSlugs;

/// <summary>
/// GET /api/v1/web/seo/slugs/{entityType} — the slug feed that backs the sitemap and static generation. Only
/// <c>content</c> is reachable today (the only module with a public slugged read); other entity types are rejected
/// as a clean error rather than faked.
/// </summary>
public sealed class GetWebSeoSlugsQuery : AizenQuery<List<WebSeoSlugItemDto>>
{
    public string EntityType { get; set; } = default!;
    public string Lang { get; set; } = "tr";
}
