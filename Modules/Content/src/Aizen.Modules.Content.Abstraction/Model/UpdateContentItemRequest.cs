using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for updating a content item's core editorial fields (§8 UpdateContentItem). Status changes
/// go through the dedicated schedule/publish/unpublish/archive endpoints, not here. Nulls mean "leave
/// unchanged" (the handler decides); translations/placements/audience/media have their own endpoints.
/// </summary>
public sealed class UpdateContentItemRequest
{
    public string? Slug { get; set; }
    public string? DefaultLanguage { get; set; }

    public List<string>? Tags { get; set; }
    public string? CategorySlug { get; set; }

    public ContentCampaignDto? Campaign { get; set; }
    public ContentReleaseNoteDto? ReleaseNote { get; set; }
}
