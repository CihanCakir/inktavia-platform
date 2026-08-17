using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for creating a content item (§8 CreateContentItem). The item is created as a Draft;
/// placements/audience/media may be supplied here or set later via their dedicated endpoints.
/// Plain contract type — validation lives in the Application validator (C4).
/// </summary>
public sealed class CreateContentItemRequest
{
    public ContentType Type { get; set; }

    /// <summary>Optional explicit slug. When null/empty, a unique slug is generated from the default title.</summary>
    public string? Slug { get; set; }

    public string DefaultLanguage { get; set; } = "tr";

    public List<ContentTranslationDto> Translations { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public string? CategorySlug { get; set; }

    public List<ContentPlacementDto> Placements { get; set; } = new();
    public ContentAudienceDto? Audience { get; set; }
    public List<ContentMediaDto> Media { get; set; } = new();

    /// <summary>Presentation block — used only when Type == Campaign.</summary>
    public ContentCampaignDto? Campaign { get; set; }

    /// <summary>Structured block — used only when Type == ReleaseNote.</summary>
    public ContentReleaseNoteDto? ReleaseNote { get; set; }

    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }
}
