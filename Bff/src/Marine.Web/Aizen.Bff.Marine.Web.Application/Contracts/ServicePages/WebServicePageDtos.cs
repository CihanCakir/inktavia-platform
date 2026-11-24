using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;

namespace Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;

/// <summary>
/// The aggregated service × location landing projection (M2 code form + M3 slug form). ONE response the page renders
/// from, never four calls: the service summary (#1) + a location summary (#3) + a COARSE availability signal.
/// Carries only the availability enum — never a provider count, ids, names, scores, or ranking.
/// </summary>
public sealed class WebServicePageDto
{
    /// <summary>The service-category tile (from the SERVICE_PROVIDER_CATEGORY catalogue), resolved from the slug.</summary>
    public WebServiceSummaryDto Service { get; set; } = default!;

    /// <summary>The location this page is scoped to.</summary>
    public WebServicePageLocationDto Location { get; set; } = default!;

    /// <summary>Coarse availability of providers for this (service, city): <c>available</c> / <c>limited</c> / <c>none</c>.</summary>
    public string Availability { get; set; } = default!;
}

/// <summary>
/// The location a service page is scoped to.
///
/// The availability read-model is city-keyed (<c>UserProfile.City</c>), so <see cref="CityCode"/> remains the key the
/// signal was computed against even when the reader arrived at a district — that has not changed.
///
/// What is NEW is <see cref="Name"/> and <see cref="Slugs"/>. D-02 requires that for a two-entity route
/// (<c>/services/[categorySlug]/[locationSlug]</c>) the single aggregated projection return slug maps for BOTH
/// entities, so the combined path can be rebuilt in each locale. Without them the locale switcher on this page can
/// only fall back to the home page, and the H1 has nothing but a raw code to print.
/// </summary>
public sealed class WebServicePageLocationDto
{
    /// <summary>The city the availability signal was computed for. Always present.</summary>
    public string CityCode { get; set; } = default!;

    /// <summary>
    /// The location the reader actually asked for — the city itself, or the district/neighbourhood that resolved to
    /// it. Null on the M2 code form, where no slug was supplied and the module has no reverse lookup from a code.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>Display name of that location. Null where only a code was supplied (M2 form) — never invented.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Per-locale slugs for the location, keyed by language code. Turkish place names are the SAME string in both
    /// locales — "Bodrum" is Bodrum in English — so both keys usually carry one value. That is not a placeholder: it
    /// is the honest answer, and emitting both keys means a consumer never has to special-case geography.
    /// Empty on the M2 code form.
    /// </summary>
    public Dictionary<string, string> Slugs { get; set; } = new();
}
