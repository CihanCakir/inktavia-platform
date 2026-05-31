using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateStreetCommandHandler : AizenCommandHandler<CreateStreetCommand, StreetDto>
{
    private readonly ILocationReferenceService _service;

    public CreateStreetCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<StreetDto?> Handle(CreateStreetCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateStreetAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, request.StreetCode, request.Name, request.PostalCode, cancellationToken);
    }
}
