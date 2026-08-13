using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetMyContentFavorites;

/// <summary>The caller's own favorites, paged, newest first (§8). Identity from the token.</summary>
public sealed class GetMyContentFavoritesQuery : AizenQuery<ContentFavoritesResponse>
{
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
