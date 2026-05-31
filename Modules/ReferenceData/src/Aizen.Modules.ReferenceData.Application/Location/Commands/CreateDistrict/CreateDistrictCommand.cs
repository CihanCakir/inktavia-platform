using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateDistrictCommand : AizenCommand<DistrictDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public Dictionary<string, string> Name { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }
    public bool IsCoastalDistrict { get; }

    public CreateDistrictCommand(string countryCode, string cityCode, string districtCode, Dictionary<string, string> name, decimal? latitude, decimal? longitude, bool isCoastalDistrict)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        Name = name;
        Latitude = latitude;
        Longitude = longitude;
        IsCoastalDistrict = isCoastalDistrict;
    }
}
