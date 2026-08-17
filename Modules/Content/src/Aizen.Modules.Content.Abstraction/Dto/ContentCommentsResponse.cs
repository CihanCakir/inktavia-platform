namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>Paged wrapper for a content item's comments (§8 GetContentComments).</summary>
public sealed class ContentCommentsResponse
{
    public List<ContentCommentDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
}
