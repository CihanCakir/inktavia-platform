namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>Paged wrapper for a participant's favorites (§8 GetMyContentFavorites).</summary>
public sealed class ContentFavoritesResponse
{
    public List<ContentFavoriteDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
}
