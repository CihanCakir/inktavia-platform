namespace Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;

/// <summary>
/// One service-category tile for the website's service catalogue grid (W4 priority 1). A web-facing reshape of the
/// ReferenceData <c>LookupItemDto</c> for the <c>SERVICE_PROVIDER_CATEGORY</c> group: the stable <see cref="Code"/>
/// and the derived URL-safe slugs are kept, the presentation fields are kept, and the internal lookup plumbing
/// (database <c>Id</c>, <c>LookupGroupId</c>, <c>GroupType</c>, <c>GroupHierarchyPath</c>, <c>IsDefault</c>,
/// <c>IsActive</c>) is OMITTED.
/// </summary>
/// <remarks>
/// This is a taxonomy grid, NOT slugged rich detail — there is deliberately no <c>Seo</c> block here: a lookup item
/// has no body, so an indexability verdict would be fabricated, and the indexable per-service editorial pages are
/// served by the content surface (<c>api/v1/web/content</c>).
///
/// It IS, however, bilingual. The lookup item carries <c>DisplayNameTr</c> alongside <c>Name</c> — the Provider and
/// Mobile BFFs have always surfaced it, and this one used to drop it, which is why a Turkish reader saw
/// "Motor Maintenance" on a Turkish page. <see cref="Name"/> is now resolved for the requested <c>lang</c> and
/// <see cref="Slugs"/> carries the per-locale slug map D-02 requires.
/// </remarks>
public sealed class WebServiceSummaryDto
{
    /// <summary>Stable service-category code (e.g. <c>ENGINE_REPAIR</c>) — the taxonomy's public key.</summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// The URL segment for the REQUESTED locale — the same value as <c>Slugs[lang]</c>, repeated here so a caller
    /// that renders one locale never has to index the map.
    /// </summary>
    public string Slug { get; set; } = default!;

    /// <summary>
    /// Every locale's slug for this entity, keyed by language code (<c>tr</c>, <c>en</c>). D-02: "every public detail
    /// projection MUST return the entity's slugs for all supported locales" — this is what lets the locale switcher
    /// land on the equivalent ENTITY rather than the home page, and what lets a consumer build the other locale's
    /// canonical URL without guessing.
    ///
    /// A locale whose source string is missing is OMITTED from the map rather than filled with a fallback, so a
    /// consumer can tell "not translated" from "translated to the same text".
    ///
    /// The English slug is derived from <see cref="Code"/>, not from the English name: a name is editorial copy and
    /// gets reworded, and a URL that moves when someone fixes a typo is a broken link plus a redirect nobody asked
    /// for. The Turkish slug has no code to derive from, so it comes from the Turkish display name.
    /// </summary>
    public Dictionary<string, string> Slugs { get; set; } = new();

    /// <summary>Display name in the requested locale (Turkish display name for <c>tr</c>, falling back to <c>Name</c>).</summary>
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>Icon reference key for the tile (no binary, just the key the frontend maps to an asset).</summary>
    public string? IconKey { get; set; }

    /// <summary>Optional accent colour for the tile (as authored in the lookup item).</summary>
    public string? ColorCode { get; set; }

    /// <summary>Authoring sort order — the grid renders ascending.</summary>
    public int SortOrder { get; set; }
}
