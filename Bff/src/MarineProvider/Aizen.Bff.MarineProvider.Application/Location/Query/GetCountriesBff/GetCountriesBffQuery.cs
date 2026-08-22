using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.MarineProvider.Application.Location;

public sealed class GetCountriesBffQuery : AizenQuery<List<CountryDto>>
{
}
