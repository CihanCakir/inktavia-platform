using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateDistrictCommandHandler : AizenCommandHandler<CreateDistrictCommand, DistrictDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateDistrictCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<DistrictDto?> Handle(CreateDistrictCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.Name, request.Latitude, request.Longitude, request.IsCoastalDistrict, cancellationToken);
        await _invalidation.InvalidateDistrictAsync(request.CountryCode, request.CityCode, cancellationToken);
        return result;
    }
}
