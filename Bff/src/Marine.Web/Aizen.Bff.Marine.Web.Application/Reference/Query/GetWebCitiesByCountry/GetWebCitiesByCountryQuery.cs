using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCitiesByCountry;

/// <summary>
/// GET /api/v1/web/reference/countries/{countryCode}/cities — active cities of a country for the public website.
/// The module DTO (<see cref="CityDto"/>) is clean reference data, so it is passed through unchanged.
/// </summary>
public sealed class GetWebCitiesByCountryQuery : AizenQuery<List<CityDto>>
{
    public string CountryCode { get; set; } = default!;
}
