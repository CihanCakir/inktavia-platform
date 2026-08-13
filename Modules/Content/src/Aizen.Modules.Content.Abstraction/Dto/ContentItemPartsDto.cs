using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Dto;

// Shared nested contract shapes for a content item. Used for BOTH read (ContentItemDto) and
// write (authoring request bodies). Plain POCOs — no Domain/Mongo types; ids surface as string.

/// <summary>A single-language rendition of a content item.</summary>
public sealed class ContentTranslationDto
{
    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}

/// <summary>Reference to a FileStorage-hosted media asset (Content stores references, never bytes).</summary>
public sealed class ContentMediaDto
{
    public string FileStorageId { get; set; } = default!;
    public string? Url { get; set; }
    public ContentMediaKind Kind { get; set; }
    public string? Alt { get; set; }
    public int Position { get; set; }
}

/// <summary>Where a content item renders — one entry per (surface, slot).</summary>
public sealed class ContentPlacementDto
{
    public ContentSurface Surface { get; set; }
    public ContentSlot Slot { get; set; }
    public int Position { get; set; }
    public DateTimeOffset? PinnedUntil { get; set; }
}

/// <summary>Who may see a content item. Region/city codes are filters only (no live geo).</summary>
public sealed class ContentAudienceDto
{
    public ContentAudienceType Type { get; set; } = ContentAudienceType.Public;
    public List<string> RegionCodes { get; set; } = new();
    public List<string> CityCodes { get; set; } = new();
    public List<string> Tiers { get; set; } = new();
}

/// <summary>Opaque outbound link to another system (e.g. Commerce campaign). Presentation only.</summary>
public sealed class ContentExternalRefDto
{
    public string System { get; set; } = default!;
    public string Key { get; set; } = default!;
}

/// <summary>Presentation block for Type=Campaign only (banner/landing CTA + optional link-out).</summary>
public sealed class ContentCampaignDto
{
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public ContentExternalRefDto? ExternalRef { get; set; }
}

/// <summary>Structured block for Type=ReleaseNote only (informational).</summary>
public sealed class ContentReleaseNoteDto
{
    public string? AppTarget { get; set; }
    public string? Version { get; set; }
    public ReleaseNotePlatform Platform { get; set; } = ReleaseNotePlatform.All;
}
