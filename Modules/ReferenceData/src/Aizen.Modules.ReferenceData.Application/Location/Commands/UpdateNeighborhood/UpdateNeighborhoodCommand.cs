using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateNeighborhoodCommand : AizenCommand<NeighborhoodDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public string NeighborhoodCode { get; }
    public string? PostalCode { get; }
    public bool IsActive { get; }

    public UpdateNeighborhoodCommand(string countryCode, string cityCode, string districtCode, string neighborhoodCode, string? postalCode, bool isActive)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        NeighborhoodCode = neighborhoodCode;
        PostalCode = postalCode;
        IsActive = isActive;
    }
}
