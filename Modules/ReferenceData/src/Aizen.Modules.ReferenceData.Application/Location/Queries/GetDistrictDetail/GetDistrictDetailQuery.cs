using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetDistrictDetailQuery : AizenQuery<DistrictDto?>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }

    public GetDistrictDetailQuery(string countryCode, string cityCode, string districtCode)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
    }
}
