using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCountryDetailQuery : AizenQuery<CountryDto?>
{
    public string CountryCode { get; }

    public GetCountryDetailQuery(string countryCode)
    {
        CountryCode = countryCode;
    }
}
