using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebDistrictDetail;

/// <summary>
/// GET /api/v1/web/locations/countries/{countryCode}/cities/{cityCode}/districts/{districtCode} — public district
/// detail (W4, PARTIAL). Resolved against the districts-by-city list (the module has no single-district read).
/// </summary>
public sealed class GetWebDistrictDetailQuery : AizenQuery<WebLocationDetailDto>
{
    public string CountryCode { get; set; } = default!;
    public string CityCode { get; set; } = default!;
    public string DistrictCode { get; set; } = default!;
}
