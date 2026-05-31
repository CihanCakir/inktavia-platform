using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetNeighborhoodsByDistrictQuery : AizenQuery<IReadOnlyList<NeighborhoodDto>>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public bool OnlyActive { get; }

    public GetNeighborhoodsByDistrictQuery(string countryCode, string cityCode, string districtCode, bool onlyActive = true)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        OnlyActive = onlyActive;
    }
}
