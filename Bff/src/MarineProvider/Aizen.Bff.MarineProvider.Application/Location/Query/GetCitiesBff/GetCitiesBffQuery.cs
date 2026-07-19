using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.MarineProvider.Application.Location;

public sealed class GetCitiesBffQuery : AizenQuery<List<CityDto>>
{
    public string Country { get; init; } = "TR";
}
