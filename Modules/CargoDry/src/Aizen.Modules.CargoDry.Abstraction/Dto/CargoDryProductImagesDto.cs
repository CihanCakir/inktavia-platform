namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// CargoDry supply flow (additive): a product's media state — the thumbnail file id + ordered gallery image file ids.
/// FileStorage file ids only; presigned display URLs are resolved at the BFF boundary. Returned by the admin
/// media-management commands (add/reorder/remove/set-thumbnail).
/// </summary>
public sealed class CargoDryProductImagesDto
{
    public Guid? ThumbnailFileId { get; init; }
    public List<CargoDryProductImageItemDto> Images { get; init; } = new();
}

public sealed class CargoDryProductImageItemDto
{
    public Guid FileId    { get; init; }
    public int  SortOrder { get; init; }
}
