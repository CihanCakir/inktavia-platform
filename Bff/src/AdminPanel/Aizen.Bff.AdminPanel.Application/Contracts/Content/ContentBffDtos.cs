namespace Aizen.Bff.AdminPanel.Application.Contracts.Content;

// BFF-side reshapes of the Content module Abstraction DTOs. Rationale for the reshape (not a straight
// re-export): the AdminPanel BFF's response serializer has no JsonStringEnumConverter (documented on the
// Payment BFF DTOs), so module enum fields are surfaced here as strings — otherwise the admin FE would
// receive opaque integers. Ids/slugs/localized-name maps pass through unchanged.

/// <summary>Full authoring detail of a content item, incl. every translation.</summary>
public sealed record ContentItemBffDto
{
    public string Id { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string Status { get; init; } = default!;

    public DateTimeOffset? PublishAt { get; init; }
    public DateTimeOffset? ExpireAt { get; init; }

    public string DefaultLanguage { get; init; } = "tr";
    public List<ContentTranslationBffDto> Translations { get; init; } = new();
    public List<ContentMediaBffDto> Media { get; init; } = new();
    public List<ContentPlacementBffDto> Placements { get; init; } = new();
    public ContentAudienceBffDto Audience { get; init; } = new();

    public List<string> Tags { get; init; } = new();
    public string? CategorySlug { get; init; }

    public ContentCampaignBffDto? Campaign { get; init; }
    public ContentReleaseNoteBffDto? ReleaseNote { get; init; }

    public int CommentCount { get; init; }
    public int FavoriteCount { get; init; }

    public long AuthorUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}

/// <summary>Lean list/feed row (one requested language).</summary>
public sealed record ContentItemSummaryBffDto
{
    public string Id { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Lang { get; init; } = default!;
    public List<string> AvailableLangs { get; init; } = new();

    public string Title { get; init; } = default!;
    public string? Summary { get; init; }
    public string? CoverUrl { get; init; }

    public string? CategorySlug { get; init; }
    public List<string> Tags { get; init; } = new();

    public int CommentCount { get; init; }
    public int FavoriteCount { get; init; }

    public DateTimeOffset? PublishAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}

/// <summary>Paged list envelope for admin item enumeration.</summary>
public sealed record ContentFeedBffResult
{
    public List<ContentItemSummaryBffDto> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long Total { get; init; }
}

public sealed record ContentTranslationBffDto
{
    public string Lang { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string? Summary { get; init; }
    public string? Body { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
}

public sealed record ContentMediaBffDto
{
    public string FileStorageId { get; init; } = default!;
    public string? Url { get; init; }
    public string Kind { get; init; } = default!;
    public string? Alt { get; init; }
    public int Position { get; init; }
}

public sealed record ContentPlacementBffDto
{
    public string Surface { get; init; } = default!;
    public string Slot { get; init; } = default!;
    public int Position { get; init; }
    public DateTimeOffset? PinnedUntil { get; init; }
}

public sealed record ContentAudienceBffDto
{
    public string Type { get; init; } = default!;
    public List<string> RegionCodes { get; init; } = new();
    public List<string> CityCodes { get; init; } = new();
    public List<string> Tiers { get; init; } = new();
}

public sealed record ContentExternalRefBffDto
{
    public string System { get; init; } = default!;
    public string Key { get; init; } = default!;
}

public sealed record ContentCampaignBffDto
{
    public string? CtaLabel { get; init; }
    public string? CtaUrl { get; init; }
    public ContentExternalRefBffDto? ExternalRef { get; init; }
}

public sealed record ContentReleaseNoteBffDto
{
    public string? AppTarget { get; init; }
    public string? Version { get; init; }
    public string Platform { get; init; } = default!;
}

/// <summary>Editorial taxonomy node.</summary>
public sealed record ContentCategoryBffDto
{
    public string Id { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public Dictionary<string, string> Name { get; init; } = new();
    public string? ParentSlug { get; init; }
    public int Position { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>A participant comment on a content item (moderation view).</summary>
public sealed record ContentCommentBffDto
{
    public string Id { get; init; } = default!;
    public string ContentId { get; init; } = default!;

    public long AuthorUserId { get; init; }
    public long? AuthorProfileId { get; init; }
    public string? AuthorDisplayName { get; init; }

    public string Body { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? ParentCommentId { get; init; }

    public long? ModeratedByUserId { get; init; }
    public DateTimeOffset? ModeratedAt { get; init; }
    public string? LastModerationReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Paged wrapper for a content item's comments.</summary>
public sealed record ContentCommentsBffResult
{
    public List<ContentCommentBffDto> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long Total { get; init; }
}
