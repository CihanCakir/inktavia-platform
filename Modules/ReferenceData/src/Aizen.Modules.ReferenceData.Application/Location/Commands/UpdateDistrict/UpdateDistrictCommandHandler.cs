using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateDistrictCommandHandler : AizenCommandHandler<UpdateDistrictCommand, DistrictDto>
{
    private readonly ILocationReferenceService _service;

    public UpdateDistrictCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<DistrictDto?> Handle(UpdateDistrictCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.Latitude, request.Longitude, request.IsCoastalDistrict, request.IsActive, cancellationToken);
    }
}
