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
    public string ServiceSlug { get; set; } = default!;

    /// <summary>Interim (M2) form: the city code directly.</summary>
    public string? CityCode { get; set; }

    /// <summary>M3 form: a location slug, resolved to its city for the (city-keyed) availability signal.</summary>
    public string? LocationSlug { get; set; }
}
