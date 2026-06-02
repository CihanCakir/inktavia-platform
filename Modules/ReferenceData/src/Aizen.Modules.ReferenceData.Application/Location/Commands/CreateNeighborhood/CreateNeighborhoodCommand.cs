using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateNeighborhoodCommand : AizenCommand<NeighborhoodDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public string NeighborhoodCode { get; }
    public Dictionary<string, string> Name { get; }
    public string? PostalCode { get; }

    public CreateNeighborhoodCommand(string countryCode, string cityCode, string districtCode, string neighborhoodCode, Dictionary<string, string> name, string? postalCode)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        NeighborhoodCode = neighborhoodCode;
        Name = name;
        PostalCode = postalCode;
    }
}
