using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCitiesByCountryQuery : AizenQuery<IReadOnlyList<CityDto>>
{
    public string CountryCode { get; }
    public bool OnlyActive { get; }

    public GetCitiesByCountryQuery(string countryCode, bool onlyActive = true)
    {
        CountryCode = countryCode;
        OnlyActive = onlyActive;
    }
}
