using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentFeed;

/// <summary>
/// GET /api/v1/web/content/feed — the MarineOsWeb published content feed (paged, localized).
/// The surface is pinned by the BFF; the caller supplies only lang/paging.
/// </summary>
public sealed class GetWebContentFeedQuery : AizenQuery<ContentFeedResponse>
{
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
