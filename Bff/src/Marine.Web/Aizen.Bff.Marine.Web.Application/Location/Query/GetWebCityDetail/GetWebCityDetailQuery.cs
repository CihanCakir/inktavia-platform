using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCityDetail;

/// <summary>GET /api/v1/web/locations/countries/{countryCode}/cities/{cityCode} — public city detail (W4, PARTIAL).</summary>
public sealed class GetWebCityDetailQuery : AizenQuery<WebLocationDetailDto>
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
}
