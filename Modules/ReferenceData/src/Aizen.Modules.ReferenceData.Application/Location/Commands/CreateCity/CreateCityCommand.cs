using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Request.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCityCommand : AizenCommand<CityDto>
{
    public CreateCityRequest Request { get; }

    public CreateCityCommand(CreateCityRequest request)
    {
        Request = request;
    }
}
