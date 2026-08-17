using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentItem;

/// <summary>Updates a content item's core editorial fields (§8). Null fields are left unchanged.</summary>
public sealed class UpdateContentItemCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;

    public string? Slug { get; set; }
    public string? DefaultLanguage { get; set; }
    public List<string>? Tags { get; set; }
    public bool ClearCategory { get; set; }
    public string? CategorySlug { get; set; }
    public ContentCampaignDto? Campaign { get; set; }
    public ContentReleaseNoteDto? ReleaseNote { get; set; }
}
