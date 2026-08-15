using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentFavorites;

/// <summary>GET /api/v1/web/me/content/favorites — the participant's own favorites (paged), enriched with a content summary.</summary>
public sealed class GetWebMyContentFavoritesQuery : AizenQuery<WebMyFavoritesResponse>
{
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
