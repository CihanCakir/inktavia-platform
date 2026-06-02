using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateCityCommand : AizenCommand<CityDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }
    public bool IsCoastalCity { get; }
    public bool IsActive { get; }

    public UpdateCityCommand(string countryCode, string cityCode, decimal? latitude, decimal? longitude, bool isCoastalCity, bool isActive)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        Latitude = latitude;
        Longitude = longitude;
        IsCoastalCity = isCoastalCity;
        IsActive = isActive;
    }
}
