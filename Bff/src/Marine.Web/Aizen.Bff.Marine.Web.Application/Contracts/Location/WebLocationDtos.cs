namespace Aizen.Bff.Marine.Web.Application.Contracts.Location;

/// <summary>
/// A public location detail for the website (W4 priority 3, PARTIAL). Reshaped from ReferenceData's
/// <c>[AllowAnonymous]</c> geography reads (country/city/district). Carries the location's own identity plus a
/// resolved <see cref="ParentChain"/> for breadcrumbs.
/// </summary>
/// <remarks>
/// PARTIAL by design — the reachable data is code-keyed, hierarchical geography only. There is NO single-slug
/// resolver and NO <c>region</c>/<c>marina</c> location type in the module today, and no per-location service list;
/// those are reported as BLOCKED in <c>docs/MARINE_WEB_BLOCKED.md</c> rather than invented here.
/// </remarks>
public sealed class WebLocationDetailDto
{
    /// <summary>One of <c>country</c> / <c>city</c> / <c>district</c> — the only reachable location types today.</summary>
    public string LocationType { get; set; } = default!;

    /// <summary>The location's own code within its parent scope (e.g. city code). Codes, not database ids.</summary>
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    /// <summary>Ancestors from the top down (country → … → immediate parent). Empty for a country.</summary>
    public List<WebLocationRefDto> ParentChain { get; set; } = new();

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>True when the geography is flagged coastal (city/district only); null where the level has no such flag.</summary>
    public bool? IsCoastal { get; set; }
}

/// <summary>A single ancestor in a location's parent chain — type + code + display name, nothing internal.</summary>
public sealed class WebLocationRefDto
{
    public string LocationType { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
