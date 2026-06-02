using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetDistrictsByCityQuery : AizenQuery<IReadOnlyList<DistrictDto>>
{
    public string CountryCode { get; }
    public string CityCode { get; }
    public bool OnlyActive { get; }

    public GetDistrictsByCityQuery(string countryCode, string cityCode, bool onlyActive = true)
    {
        CountryCode = countryCode;
        CityCode = cityCode;
        OnlyActive = onlyActive;
    }
}
