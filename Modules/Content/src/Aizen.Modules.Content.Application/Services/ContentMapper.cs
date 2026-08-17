using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Domain.ValueObjects;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Document ↔ contract mapping for the Content module. Kept here (Application) so handlers stay thin;
/// the Abstraction layer holds only the shapes (C3).
/// </summary>
public static class ContentMapper
{
    // ── Read: document → DTO ──────────────────────────────────────────────────

    public static ContentItemDto ToDto(ContentItemDocument d) => new()
    {
        Id = d.Id,
        Type = d.Type,
        Slug = d.Slug,
        Status = d.Status,
        PublishAt = d.PublishAt,
        ExpireAt = d.ExpireAt,
        DefaultLanguage = d.DefaultLanguage,
        Translations = d.Translations.Select(ToDto).ToList(),
        Media = d.Media.Select(ToDto).ToList(),
        Placements = d.Placements.Select(ToDto).ToList(),
        Audience = ToDto(d.Audience),
        Tags = new List<string>(d.Tags),
        CategorySlug = d.CategorySlug,
        Campaign = d.Campaign is null ? null : ToDto(d.Campaign),
        ReleaseNote = d.ReleaseNote is null ? null : ToDto(d.ReleaseNote),
        CommentCount = d.CommentCount,
        FavoriteCount = d.FavoriteCount,
        AuthorUserId = d.AuthorUserId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        PublishedAt = d.PublishedAt,
    };

    public static ContentItemSummaryDto ToSummary(ContentItemDocument d, string lang)
    {
        var tr = ResolveTranslation(d, lang);
        return new ContentItemSummaryDto
        {
            Id = d.Id,
            Type = d.Type,
            Slug = d.Slug,
            Status = d.Status,
            Lang = tr?.Lang ?? d.DefaultLanguage,
            AvailableLangs = d.Translations.Select(t => t.Lang).ToList(),
            Title = tr?.Title ?? d.Slug,
            Summary = tr?.Summary,
            CoverUrl = ResolveCoverUrl(d),
            CategorySlug = d.CategorySlug,
            Tags = new List<string>(d.Tags),
            CommentCount = d.CommentCount,
            FavoriteCount = d.FavoriteCount,
            PublishAt = d.PublishAt,
            PublishedAt = d.PublishedAt,
        };
    }

    public static ContentCategoryDto ToDto(ContentCategoryDocument d) => new()
    {
        Id = d.Id,
        Slug = d.Slug,
        Name = new Dictionary<string, string>(d.Name),
        ParentSlug = d.ParentSlug,
        Position = d.Position,
        IsActive = d.IsActive,
    };

    public static ContentCommentDto ToDto(ContentCommentDocument d) => new()
    {
        Id = d.Id,
        ContentId = d.ContentId,
        AuthorUserId = d.AuthorUserId,
        AuthorProfileId = d.AuthorProfileId,
        AuthorDisplayName = d.AuthorDisplayName,
        Body = d.Body,
        Status = d.Status,
        ParentCommentId = d.ParentCommentId,
        ModeratedByUserId = d.ModeratedByUserId,
        ModeratedAt = d.ModeratedAt,
        LastModerationReason = d.LastModerationReason,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };

    public static ContentFavoriteDto ToDto(ContentFavoriteDocument d) => new()
    {
        Id = d.Id,
        ContentId = d.ContentId,
        UserId = d.UserId,
        ProfileId = d.ProfileId,
        CreatedAt = d.CreatedAt,
    };

    private static ContentTranslation? ResolveTranslation(ContentItemDocument d, string lang)
        => d.Translations.FirstOrDefault(t => string.Equals(t.Lang, lang, StringComparison.OrdinalIgnoreCase))
           ?? d.Translations.FirstOrDefault(t => string.Equals(t.Lang, d.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
           ?? d.Translations.FirstOrDefault();

    private static string? ResolveCoverUrl(ContentItemDocument d)
        => d.Media.Where(m => m.Kind == ContentMediaKind.Cover).OrderBy(m => m.Position).FirstOrDefault()?.Url
           ?? d.Media.OrderBy(m => m.Position).FirstOrDefault()?.Url;

    private static ContentTranslationDto ToDto(ContentTranslation t) => new()
    {
        Lang = t.Lang, Title = t.Title, Summary = t.Summary, Body = t.Body,
        SeoTitle = t.SeoTitle, SeoDescription = t.SeoDescription,
    };

    private static ContentMediaDto ToDto(ContentMediaRef m) => new()
    {
        FileStorageId = m.FileStorageId, Url = m.Url, Kind = m.Kind, Alt = m.Alt, Position = m.Position,
    };

    private static ContentPlacementDto ToDto(ContentPlacement p) => new()
    {
        Surface = p.Surface, Slot = p.Slot, Position = p.Position, PinnedUntil = p.PinnedUntil,
    };

    private static ContentAudienceDto ToDto(ContentAudience a) => new()
    {
        Type = a.Type,
        RegionCodes = new List<string>(a.RegionCodes),
        CityCodes = new List<string>(a.CityCodes),
        Tiers = new List<string>(a.Tiers),
    };

    private static ContentCampaignDto ToDto(ContentCampaignBlock c) => new()
    {
        CtaLabel = c.CtaLabel, CtaUrl = c.CtaUrl,
        ExternalRef = c.ExternalRef is null ? null : new ContentExternalRefDto { System = c.ExternalRef.System, Key = c.ExternalRef.Key },
    };

    private static ContentReleaseNoteDto ToDto(ContentReleaseNoteBlock r) => new()
    {
        AppTarget = r.AppTarget, Version = r.Version, Platform = r.Platform,
    };

    // ── Write: contract part → domain value object ────────────────────────────

    public static ContentTranslation ToVo(ContentTranslationDto t) => new()
    {
        Lang = t.Lang, Title = t.Title, Summary = t.Summary, Body = t.Body,
        SeoTitle = t.SeoTitle, SeoDescription = t.SeoDescription,
    };

    public static ContentMediaRef ToVo(ContentMediaDto m) => new()
    {
        FileStorageId = m.FileStorageId, Url = m.Url, Kind = m.Kind, Alt = m.Alt, Position = m.Position,
    };

    public static ContentPlacement ToVo(ContentPlacementDto p) => new()
    {
        Surface = p.Surface, Slot = p.Slot, Position = p.Position, PinnedUntil = p.PinnedUntil,
    };

    public static ContentAudience ToVo(ContentAudienceDto a) => new()
    {
        Type = a.Type,
        RegionCodes = new List<string>(a.RegionCodes),
        CityCodes = new List<string>(a.CityCodes),
        Tiers = new List<string>(a.Tiers),
    };

    public static ContentCampaignBlock ToVo(ContentCampaignDto c) => new()
    {
        CtaLabel = c.CtaLabel, CtaUrl = c.CtaUrl,
        ExternalRef = c.ExternalRef is null ? null : new ContentExternalRef { System = c.ExternalRef.System, Key = c.ExternalRef.Key },
    };

    public static ContentReleaseNoteBlock ToVo(ContentReleaseNoteDto r) => new()
    {
        AppTarget = r.AppTarget, Version = r.Version, Platform = r.Platform,
    };
}
