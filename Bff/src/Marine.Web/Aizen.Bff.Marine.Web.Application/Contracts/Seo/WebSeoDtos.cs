namespace Aizen.Bff.Marine.Web.Application.Contracts.Seo;

/// <summary>
/// The SEO block carried by every indexable web projection (W3.2). <see cref="Indexable"/> is the backend's verdict
/// and <see cref="Reason"/> explains it; Title/Description are the SEO-preferred strings (falling back to the
/// content title/summary). Carries nothing sensitive.
/// </summary>
public sealed class WebSeoDto
{
    public bool Indexable { get; set; }
    public string Reason { get; set; } = default!;
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// One row of the SEO slug feed (W3.3) that backs the sitemap and static generation. <see cref="LastModified"/> is
/// the item's PublishedAt — see the README precision caveat (the feed summary has no UpdatedAt).
/// </summary>
public sealed class WebSeoSlugItemDto
{
    public string Slug { get; set; } = default!;
    public List<string> AvailableLangs { get; set; } = new();
    public DateTimeOffset? LastModified { get; set; }
    public bool Indexable { get; set; }
}
