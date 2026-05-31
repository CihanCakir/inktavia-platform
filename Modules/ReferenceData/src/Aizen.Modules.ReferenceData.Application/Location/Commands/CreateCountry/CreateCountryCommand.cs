using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Request.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCountryCommand : AizenCommand<CountryDto>
{
    public CreateCountryRequest Request { get; }

    public CreateCountryCommand(CreateCountryRequest request)
    {
        Request = request;
    }
}
