using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateDistrictCommandHandler : AizenCommandHandler<CreateDistrictCommand, DistrictDto>
{
    private readonly ILocationReferenceService _service;

    public CreateDistrictCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<DistrictDto?> Handle(CreateDistrictCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.Name, request.Latitude, request.Longitude, request.IsCoastalDistrict, cancellationToken);
    }
}
