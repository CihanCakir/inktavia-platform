using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.Marine.Web.Application.Content;

/// <summary>
/// Module Content DTO → web DTO mapping. Only the by-slug detail and the comments list are reshaped (they leak
/// internal fields); the feed (<c>ContentFeedResponse</c>/<c>ContentItemSummaryDto</c>) and category list
/// (<c>ContentCategoryDto</c>) are already web-appropriate and pass through unmapped.
/// </summary>
public static class WebContentMapper
{
    public static WebContentDetailDto ToDetail(ContentItemDto d, string lang)
    {
        var tr = ResolveTranslation(d, lang);
        return new WebContentDetailDto
        {
            Id = d.Id,
            Type = d.Type,
            Slug = d.Slug,
            Lang = tr?.Lang ?? d.DefaultLanguage,
            Title = tr?.Title ?? d.Slug,
            Summary = tr?.Summary,
            Body = tr?.Body,
            SeoTitle = tr?.SeoTitle,
            SeoDescription = tr?.SeoDescription,
            CoverUrl = ResolveCoverUrl(d),
            Gallery = d.Media
                .Where(m => m.Kind == ContentMediaKind.Gallery)
                .OrderBy(m => m.Position)
                .Select(m => new WebContentMediaDto { Url = m.Url, Alt = m.Alt, Kind = m.Kind, Position = m.Position })
                .ToList(),
            // W3.1 — the item already carries its full translation set here (detail); expose the language codes for
            // hreflang. The Seo block is set by the handler (via the indexability policy), not this static mapper.
            AvailableLangs = d.Translations.Select(t => t.Lang).ToList(),
            Tags = new List<string>(d.Tags),
            CategorySlug = d.CategorySlug,
            Campaign = d.Campaign is null ? null : new WebContentCampaignDto
            {
                CtaLabel = d.Campaign.CtaLabel,
                CtaUrl = d.Campaign.CtaUrl,
                ExternalSystem = d.Campaign.ExternalRef?.System,
                ExternalKey = d.Campaign.ExternalRef?.Key,
            },
            ReleaseNote = d.ReleaseNote is null ? null : new WebContentReleaseNoteDto
            {
                AppTarget = d.ReleaseNote.AppTarget,
                Version = d.ReleaseNote.Version,
                Platform = d.ReleaseNote.Platform,
            },
            CommentCount = d.CommentCount,
            FavoriteCount = d.FavoriteCount,
            PublishedAt = d.PublishedAt,
        };
    }

    // ── /me engagement (W2) ───────────────────────────────────────────────────

    public static WebMyCommentDto ToMyComment(ContentCommentDto c) => new()
    {
        Id = c.Id,
        ContentId = c.ContentId,
        Body = c.Body,
        Status = c.Status,           // the caller's OWN status is intentionally exposed
        ParentCommentId = c.ParentCommentId,
        CreatedAt = c.CreatedAt,
    };

    public static WebMyFavoritesResponse ToMyFavorites(ContentFavoritesResponse r) => new()
    {
        Items = r.Items.Select(f => new WebMyFavoriteDto
        {
            Id = f.Id,
            ContentId = f.ContentId,
            CreatedAt = f.CreatedAt,
            Content = f.Content,     // ContentItemSummaryDto is already web-appropriate (W1 passthrough)
        }).ToList(),
        Page = r.Page,
        PageSize = r.PageSize,
        Total = r.Total,
    };

    public static WebContentCommentsResponse ToComments(ContentCommentsResponse r) => new()
    {
        Items = r.Items.Select(c => new WebContentCommentDto
        {
            Id = c.Id,
            AuthorDisplayName = c.AuthorDisplayName,
            Body = c.Body,
            ParentCommentId = c.ParentCommentId,
            CreatedAt = c.CreatedAt,
        }).ToList(),
        Page = r.Page,
        PageSize = r.PageSize,
        Total = r.Total,
    };

    private static ContentTranslationDto? ResolveTranslation(ContentItemDto d, string lang)
        => d.Translations.FirstOrDefault(t => string.Equals(t.Lang, lang, StringComparison.OrdinalIgnoreCase))
           ?? d.Translations.FirstOrDefault(t => string.Equals(t.Lang, d.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
           ?? d.Translations.FirstOrDefault();

    private static string? ResolveCoverUrl(ContentItemDto d)
        => d.Media.Where(m => m.Kind == ContentMediaKind.Cover).OrderBy(m => m.Position).FirstOrDefault()?.Url
           ?? d.Media.OrderBy(m => m.Position).FirstOrDefault()?.Url;
}
