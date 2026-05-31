using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCityDetailQuery : AizenQuery<CityDto?>
{
    public string CountryCode { get; }
    public string CityCode { get; }

    public GetCityDetailQuery(string countryCode, string cityCode)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
    }
}
