using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCountryDetail;

/// <summary>GET /api/v1/web/locations/countries/{countryCode} — public country detail (W4, PARTIAL).</summary>
public sealed class GetWebCountryDetailQuery : AizenQuery<WebLocationDetailDto>
{
    public string CountryCode { get; set; } = default!;
}
