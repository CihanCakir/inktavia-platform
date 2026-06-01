using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateDistrictCommandHandler : AizenCommandHandler<UpdateDistrictCommand, DistrictDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateDistrictCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<DistrictDto?> Handle(UpdateDistrictCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.Latitude, request.Longitude, request.IsCoastalDistrict, request.IsActive, cancellationToken);
        await _invalidation.InvalidateDistrictAsync(request.CountryCode, request.CityCode, cancellationToken);
        return result;
    }
}
