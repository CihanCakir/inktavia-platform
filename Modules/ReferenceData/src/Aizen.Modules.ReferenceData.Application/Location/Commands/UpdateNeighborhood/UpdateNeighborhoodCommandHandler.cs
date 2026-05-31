using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateNeighborhoodCommandHandler : AizenCommandHandler<UpdateNeighborhoodCommand, NeighborhoodDto>
{
    private readonly ILocationReferenceService _service;

    public UpdateNeighborhoodCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<NeighborhoodDto?> Handle(UpdateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateNeighborhoodAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, request.PostalCode, request.IsActive, cancellationToken);
    }
}
