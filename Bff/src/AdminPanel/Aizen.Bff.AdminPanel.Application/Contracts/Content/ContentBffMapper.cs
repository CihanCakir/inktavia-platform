using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Contracts.Content;

/// <summary>
/// Maps Content module Abstraction DTOs → AdminPanel BFF DTOs. Enum fields are surfaced as their
/// string name (see ContentBffDtos rationale). Null-tolerant: a null module DTO maps to null.
/// </summary>
public static class ContentBffMapper
{
    public static ContentFeedBffResult ToBff(ContentFeedResponse? src) =>
        src is null
            ? new ContentFeedBffResult()
            : new ContentFeedBffResult
            {
                Items = src.Items.Select(ToBff).ToList(),
                Page = src.Page,
                PageSize = src.PageSize,
                Total = src.Total,
            };

    public static ContentItemSummaryBffDto ToBff(ContentItemSummaryDto s) => new()
    {
        Id = s.Id,
        Type = s.Type.ToString(),
        Slug = s.Slug,
        Status = s.Status.ToString(),
        Lang = s.Lang,
        AvailableLangs = s.AvailableLangs,
        Title = s.Title,
        Summary = s.Summary,
        CoverUrl = s.CoverUrl,
        CategorySlug = s.CategorySlug,
        Tags = s.Tags,
        CommentCount = s.CommentCount,
        FavoriteCount = s.FavoriteCount,
        PublishAt = s.PublishAt,
        PublishedAt = s.PublishedAt,
    };

    public static ContentItemBffDto? ToBff(ContentItemDto? i) =>
        i is null
            ? null
            : new ContentItemBffDto
            {
                Id = i.Id,
                Type = i.Type.ToString(),
                Slug = i.Slug,
                Status = i.Status.ToString(),
                PublishAt = i.PublishAt,
                ExpireAt = i.ExpireAt,
                DefaultLanguage = i.DefaultLanguage,
                Translations = i.Translations.Select(ToBff).ToList(),
                Media = i.Media.Select(ToBff).ToList(),
                Placements = i.Placements.Select(ToBff).ToList(),
                Audience = ToBff(i.Audience),
                Tags = i.Tags,
                CategorySlug = i.CategorySlug,
                Campaign = ToBff(i.Campaign),
                ReleaseNote = ToBff(i.ReleaseNote),
                CommentCount = i.CommentCount,
                FavoriteCount = i.FavoriteCount,
                AuthorUserId = i.AuthorUserId,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                PublishedAt = i.PublishedAt,
            };

    public static ContentTranslationBffDto ToBff(ContentTranslationDto t) => new()
    {
        Lang = t.Lang,
        Title = t.Title,
        Summary = t.Summary,
        Body = t.Body,
        SeoTitle = t.SeoTitle,
        SeoDescription = t.SeoDescription,
    };

    public static ContentMediaBffDto ToBff(ContentMediaDto m) => new()
    {
        FileStorageId = m.FileStorageId,
        Url = m.Url,
        Kind = m.Kind.ToString(),
        Alt = m.Alt,
        Position = m.Position,
    };

    public static ContentPlacementBffDto ToBff(ContentPlacementDto p) => new()
    {
        Surface = p.Surface.ToString(),
        Slot = p.Slot.ToString(),
        Position = p.Position,
        PinnedUntil = p.PinnedUntil,
    };

    public static ContentAudienceBffDto ToBff(ContentAudienceDto? a) =>
        a is null
            ? new ContentAudienceBffDto { Type = Modules.Content.Abstraction.Enum.ContentAudienceType.Public.ToString() }
            : new ContentAudienceBffDto
            {
                Type = a.Type.ToString(),
                RegionCodes = a.RegionCodes,
                CityCodes = a.CityCodes,
                Tiers = a.Tiers,
            };

    public static ContentCampaignBffDto? ToBff(ContentCampaignDto? c) =>
        c is null
            ? null
            : new ContentCampaignBffDto
            {
                CtaLabel = c.CtaLabel,
                CtaUrl = c.CtaUrl,
                ExternalRef = c.ExternalRef is null
                    ? null
                    : new ContentExternalRefBffDto { System = c.ExternalRef.System, Key = c.ExternalRef.Key },
            };

    public static ContentReleaseNoteBffDto? ToBff(ContentReleaseNoteDto? r) =>
        r is null
            ? null
            : new ContentReleaseNoteBffDto
            {
                AppTarget = r.AppTarget,
                Version = r.Version,
                Platform = r.Platform.ToString(),
            };

    public static ContentCategoryBffDto ToBff(ContentCategoryDto c) => new()
    {
        Id = c.Id,
        Slug = c.Slug,
        Name = c.Name,
        ParentSlug = c.ParentSlug,
        Position = c.Position,
        IsActive = c.IsActive,
    };

    public static ContentCommentsBffResult ToBff(ContentCommentsResponse? src) =>
        src is null
            ? new ContentCommentsBffResult()
            : new ContentCommentsBffResult
            {
                Items = src.Items.Select(ToBff).ToList(),
                Page = src.Page,
                PageSize = src.PageSize,
                Total = src.Total,
            };

    public static ContentCommentBffDto ToBff(ContentCommentDto c) => new()
    {
        Id = c.Id,
        ContentId = c.ContentId,
        AuthorUserId = c.AuthorUserId,
        AuthorProfileId = c.AuthorProfileId,
        AuthorDisplayName = c.AuthorDisplayName,
        Body = c.Body,
        Status = c.Status.ToString(),
        ParentCommentId = c.ParentCommentId,
        ModeratedByUserId = c.ModeratedByUserId,
        ModeratedAt = c.ModeratedAt,
        LastModerationReason = c.LastModerationReason,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}
