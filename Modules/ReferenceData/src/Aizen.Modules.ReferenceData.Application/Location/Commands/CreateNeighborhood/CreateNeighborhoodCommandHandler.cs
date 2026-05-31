using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateNeighborhoodCommandHandler : AizenCommandHandler<CreateNeighborhoodCommand, NeighborhoodDto>
{
    private readonly ILocationReferenceService _service;

    public CreateNeighborhoodCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<NeighborhoodDto?> Handle(CreateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateNeighborhoodAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, request.Name, request.PostalCode, cancellationToken);
    }
}
