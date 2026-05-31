using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateStreetCommand : AizenCommand<StreetDto>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public string DistrictCode { get; }
    public string? NeighborhoodCode { get; }
    public string StreetCode { get; }
    public Dictionary<string, string> Name { get; }
    public string? PostalCode { get; }

    public CreateStreetCommand(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, string streetCode, Dictionary<string, string> name, string? postalCode)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        DistrictCode = districtCode;
        NeighborhoodCode = neighborhoodCode;
        StreetCode = streetCode;
        Name = name;
        PostalCode = postalCode;
    }
}
