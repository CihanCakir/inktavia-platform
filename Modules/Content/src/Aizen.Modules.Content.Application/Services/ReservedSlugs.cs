namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Slugs a public site resolves as static route segments BEFORE dynamic entity routes. An entity slugged with one
/// of these (or a localized equivalent) becomes permanently, silently unreachable, so authoring rejects them at
/// slug-creation time (W3.4). Single source of truth — referenced by the create/update validators and by
/// <see cref="SlugService"/> auto-generation.
/// </summary>
public static class ReservedSlugs
{
    /// <summary>
    /// Technical/route segments (locale-independent) + Turkish equivalents of the user-facing ones. Extend when the
    /// frontend adds a localized static segment. Comparison is case-insensitive against the already-slugified value.
    /// </summary>
    public static readonly IReadOnlySet<string> Values = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Technical / framework / SEO endpoints — never localized.
        "api", "admin", "index", "sitemap", "robots", "assets", "static", "_next",
        // User-facing static segments (English).
        "search", "request", "new", "all", "compare",
        // Turkish equivalents of the user-facing segments.
        "ara", "talep", "yeni", "tumu", "karsilastir",
    };

    public static bool IsReserved(string? slug)
        => !string.IsNullOrWhiteSpace(slug) && Values.Contains(slug.Trim());
}
