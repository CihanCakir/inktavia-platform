using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCityCommandHandler : AizenCommandHandler<CreateCityCommand, CityDto>
{
    private readonly ILocationReferenceService _service;

    public CreateCityCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CityDto?> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateCityAsync(request.Request, cancellationToken);
    }
}
