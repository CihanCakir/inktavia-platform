using Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.ServicePages.Query.GetWebServicePage;

/// <summary>
/// The aggregated service × location landing. Two forms feed it: the M2 interim
/// <c>{serviceSlug}?cityCode={cityCode}</c> and the M3 <c>{serviceSlug}/{locationSlug}</c> (location resolved via the
/// flat slug resolver, then walked to its city — availability stays city-keyed). Exactly one of
/// <see cref="CityCode"/> / <see cref="LocationSlug"/> is supplied.
/// </summary>
public sealed class GetWebServicePageQuery : AizenQuery<WebServicePageDto>
{
    /// <summary>
    /// The service slug from the URL, in WHICHEVER locale the reader arrived in. The resolver matches it against
    /// every locale's slug, so <c>/tr/hizmetler/motor-bakimi</c> and <c>/en/services/motor-maintenance</c> resolve to
    /// the same catalogue item.
    /// </summary>
    public string ServiceSlug { get; set; } = default!;

    /// <summary>Interim (M2) form: the city code directly.</summary>
    public string? CityCode { get; set; }

    /// <summary>M3 form: a location slug, resolved to its city for the (city-keyed) availability signal.</summary>
    public string? LocationSlug { get; set; }

    /// <summary>Display locale for the embedded service summary. Absent resolves to the default.</summary>
    public string? Lang { get; set; }
}
