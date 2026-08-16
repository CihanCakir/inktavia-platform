using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;

namespace Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;

/// <summary>
/// The aggregated service × location landing projection (M2, interim code form). ONE response the page renders from,
/// never four calls: the service summary (#1) + a location summary (#3) + a COARSE availability signal. Carries only
/// the availability enum — never a provider count, ids, names, scores, or ranking.
/// </summary>
public sealed class WebServicePageDto
{
    /// <summary>The service-category tile (from the SERVICE_PROVIDER_CATEGORY catalogue), resolved from the slug.</summary>
    public WebServiceSummaryDto Service { get; set; } = default!;

    /// <summary>The location this page is scoped to. Interim: city code only (M3 upgrades to a full location).</summary>
    public WebServicePageLocationDto Location { get; set; } = default!;

    /// <summary>Coarse availability of providers for this (service, city): <c>available</c> / <c>limited</c> / <c>none</c>.</summary>
    public string Availability { get; set; } = default!;
}

/// <summary>
/// Interim location summary for a service page — the city code the page is scoped to. The availability read-model is
/// city-keyed (<c>UserProfile.City</c>), so district/marina scoping is not available; when M3 ships the location-slug
/// resolver this upgrades to the full location detail (name + parentChain).
/// </summary>
public sealed class WebServicePageLocationDto
{
    public string CityCode { get; set; } = default!;
}
