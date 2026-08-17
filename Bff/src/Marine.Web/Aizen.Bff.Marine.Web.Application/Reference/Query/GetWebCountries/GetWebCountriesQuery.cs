using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCountries;

/// <summary>
/// GET /api/v1/web/reference/countries — active countries for the public website (phone codes, currency, etc.).
/// The module DTO (<see cref="CountryDto"/>) is clean reference data with nothing sensitive, so it is passed
/// through unchanged.
/// </summary>
public sealed class GetWebCountriesQuery : AizenQuery<List<CountryDto>>
{
}
