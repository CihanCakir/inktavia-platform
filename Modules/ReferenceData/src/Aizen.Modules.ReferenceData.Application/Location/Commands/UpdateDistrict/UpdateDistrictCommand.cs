using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateDistrictCommand : AizenCommand<DistrictDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }
    public bool IsCoastalDistrict { get; }
    public bool IsActive { get; }

    public UpdateDistrictCommand(string countryCode, string cityCode, string districtCode, decimal? latitude, decimal? longitude, bool isCoastalDistrict, bool isActive)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        Latitude = latitude;
        Longitude = longitude;
        IsCoastalDistrict = isCoastalDistrict;
        IsActive = isActive;
    }
}
