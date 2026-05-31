using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateCityCommandHandler : AizenCommandHandler<UpdateCityCommand, CityDto>
{
    private readonly ILocationReferenceService _service;

    public UpdateCityCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CityDto?> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateCityAsync(request.CountryCode, request.CityCode, request.Latitude, request.Longitude, request.IsCoastalCity, request.IsActive, cancellationToken);
    }
}
