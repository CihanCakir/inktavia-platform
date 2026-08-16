namespace Aizen.Modules.ReferenceData.Repository.Mongo;

/// <summary>
/// M3 — reserved slugs a public site resolves as static route segments before dynamic entity routes (W3.4). A
/// location must never take one, so the backfill treats these as already-used and suffixes any collision. Canonical
/// source of the rule is the Content module's <c>ReservedSlugs</c>; this mirrors it (no cross-module reference).
/// </summary>
public static class LocationReservedSlugs
{
    public static readonly IReadOnlySet<string> Values = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Technical / framework / SEO endpoints — never localized.
        "api", "admin", "index", "sitemap", "robots", "assets", "static", "_next",
        // User-facing static segments (English).
        "search", "request", "new", "all", "compare",
        // Turkish equivalents of the user-facing segments.
        "ara", "talep", "yeni", "tumu", "karsilastir",
    };
}
