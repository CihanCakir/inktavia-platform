namespace Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;

/// <summary>
/// One service-category tile for the website's service catalogue grid (W4 priority 1). A web-facing reshape of the
/// ReferenceData <c>LookupItemDto</c> for the <c>SERVICE_PROVIDER_CATEGORY</c> group: the stable <see cref="Code"/>
/// and a derived URL-safe <see cref="Slug"/> are kept, the presentation fields are kept, and the internal lookup
/// plumbing (database <c>Id</c>, <c>LookupGroupId</c>, <c>GroupType</c>, <c>GroupHierarchyPath</c>, <c>IsDefault</c>,
/// <c>IsActive</c>) is OMITTED.
/// </summary>
/// <remarks>
/// This is a taxonomy grid, NOT slugged rich detail — there is deliberately no <c>Seo</c> block or
/// <c>availableLangs</c> here: a lookup item carries a single <c>Name</c>/<c>Description</c> with no translation set,
/// and the indexable per-service editorial pages are served by the content surface (<c>api/v1/web/content</c>), not
/// this catalogue. Advertising an SEO verdict or locale list we do not have would be fabricated data.
/// </remarks>
public sealed class WebServiceSummaryDto
{
    /// <summary>Stable service-category code (e.g. <c>ENGINE_REPAIR</c>) — the taxonomy's public key.</summary>
    public string Code { get; set; } = default!;

    /// <summary>URL-safe segment derived deterministically from <see cref="Code"/> (e.g. <c>engine-repair</c>).</summary>
    public string Slug { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>Icon reference key for the tile (no binary, just the key the frontend maps to an asset).</summary>
    public string? IconKey { get; set; }

    /// <summary>Optional accent colour for the tile (as authored in the lookup item).</summary>
    public string? ColorCode { get; set; }

    /// <summary>Authoring sort order — the grid renders ascending.</summary>
    public int SortOrder { get; set; }
}
