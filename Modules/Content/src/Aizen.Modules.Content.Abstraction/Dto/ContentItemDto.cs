using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>
/// Full read model of a content item (§5.1), returned by admin get-by-id and detail reads.
/// Ids surface as string; no Domain/Mongo types leak through this contract.
/// </summary>
public sealed class ContentItemDto
{
    public string Id { get; set; } = default!;
    public ContentType Type { get; set; }
    public string Slug { get; set; } = default!;
    public ContentStatus Status { get; set; }

    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }

    public string DefaultLanguage { get; set; } = "tr";
    public List<ContentTranslationDto> Translations { get; set; } = new();
    public List<ContentMediaDto> Media { get; set; } = new();
    public List<ContentPlacementDto> Placements { get; set; } = new();
    public ContentAudienceDto Audience { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public string? CategorySlug { get; set; }

    /// <summary>Populated only when Type == Campaign.</summary>
    public ContentCampaignDto? Campaign { get; set; }

    /// <summary>Populated only when Type == ReleaseNote.</summary>
    public ContentReleaseNoteDto? ReleaseNote { get; set; }

    public int CommentCount { get; set; }
    public int FavoriteCount { get; set; }

    public long AuthorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
