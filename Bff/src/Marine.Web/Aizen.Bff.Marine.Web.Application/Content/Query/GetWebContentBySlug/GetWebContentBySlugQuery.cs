using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;

/// <summary>
/// GET /api/v1/web/content/items/{slug} — the localized public detail of one published item.
/// A not-visible slug surfaces as a clean "not found" business error.
/// </summary>
public sealed class GetWebContentBySlugQuery : AizenQuery<WebContentDetailDto>
{
    public string Slug { get; set; } = default!;
    public string Lang { get; set; } = "tr";
}
