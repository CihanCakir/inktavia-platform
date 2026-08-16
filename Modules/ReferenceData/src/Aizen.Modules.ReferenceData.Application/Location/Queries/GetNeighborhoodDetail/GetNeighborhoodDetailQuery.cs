using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetNeighborhoodDetailQuery : AizenQuery<NeighborhoodDto?>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public string NeighborhoodCode { get; }

    public GetNeighborhoodDetailQuery(string countryCode, string cityCode, string districtCode, string neighborhoodCode)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        NeighborhoodCode = neighborhoodCode;
    }
}
