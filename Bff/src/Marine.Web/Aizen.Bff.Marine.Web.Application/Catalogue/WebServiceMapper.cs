using Aizen.Bff.Marine.Web.Application.Common.Text;
using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Web.Application.Catalogue;

/// <summary>
/// ReferenceData <c>LookupItemDto</c> (SERVICE_PROVIDER_CATEGORY group) → web service-catalogue DTO. Strips the
/// internal lookup plumbing, resolves the display name for the requested locale, and derives the per-locale slug
/// map. Field-stripping proven in W5.
/// </summary>
public static class WebServiceMapper
{
    /// <summary>Maps with the default locale — the shape an existing caller that sends no <c>lang</c> still gets.</summary>
    public static WebServiceSummaryDto ToSummary(LookupItemDto i) => ToSummary(i, WebLocale.Default);

    public static WebServiceSummaryDto ToSummary(LookupItemDto i, string? lang)
    {
        var locale = WebLocale.Normalize(lang);
        var slugs = BuildSlugs(i);

        return new WebServiceSummaryDto
        {
            Code = i.Code,
            Slugs = slugs,
            // The requested locale's slug, falling back to the English one and finally to the raw code transform, so
            // `Slug` is never empty even for an item with no translation yet.
            Slug = slugs.TryGetValue(locale, out var own) ? own
                : slugs.TryGetValue(WebLocale.English, out var en) ? en
                : WebLocale.ToSlug(i.Code),
            Name = ResolveName(i, locale),
            Description = i.Description,
            IconKey = i.IconKey,
            ColorCode = i.ColorCode,
            SortOrder = i.SortOrder,
        };
    }

    /// <summary>
    /// The locale's display name. Turkish reads <c>DisplayNameTr</c> and falls back to <c>Name</c> when the item has
    /// not been translated — showing the English name is honest; inventing a Turkish one is not.
    /// </summary>
    public static string ResolveName(LookupItemDto i, string locale) =>
        locale == WebLocale.Turkish && !string.IsNullOrWhiteSpace(i.DisplayNameTr)
            ? i.DisplayNameTr!
            : i.Name;

    /// <summary>
    /// The per-locale slug map. English is derived from the stable <c>Code</c>; Turkish from <c>DisplayNameTr</c>,
    /// and is OMITTED when the item carries no Turkish name — an absent key means "not translated", which is
    /// information the consumer needs (D-02: a locale with no slug is omitted, not faked).
    /// </summary>
    public static Dictionary<string, string> BuildSlugs(LookupItemDto i)
    {
        var slugs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var en = WebLocale.ToSlug(i.Code);
        if (!string.IsNullOrEmpty(en))
            slugs[WebLocale.English] = en;

        var tr = WebLocale.ToSlug(i.DisplayNameTr);
        if (!string.IsNullOrEmpty(tr))
            slugs[WebLocale.Turkish] = tr;

        return slugs;
    }

    /// <summary>
    /// True when <paramref name="slug"/> is ANY of this item's locale slugs. The reverse lookup the service-page
    /// resolver needs: a Turkish URL carries the Turkish slug, an English URL the English one, and both must resolve
    /// to the same catalogue item.
    /// </summary>
    public static bool MatchesSlug(LookupItemDto i, string slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && BuildSlugs(i).Values.Any(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Deterministic code → URL segment. Retained as the published name of the transform (W4 callers and tests use
    /// it); the implementation now lives in <see cref="WebLocale.ToSlug"/> so the Turkish fold is shared by every
    /// locale-aware projection instead of being reimplemented per surface.
    /// </summary>
    public static string ToSlug(string code) => WebLocale.ToSlug(code);
}
