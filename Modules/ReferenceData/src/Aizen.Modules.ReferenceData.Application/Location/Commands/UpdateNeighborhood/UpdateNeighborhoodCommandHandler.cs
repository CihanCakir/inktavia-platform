using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateNeighborhoodCommandHandler : AizenCommandHandler<UpdateNeighborhoodCommand, NeighborhoodDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateNeighborhoodCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<NeighborhoodDto?> Handle(UpdateNeighborhoodCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateNeighborhoodAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, request.PostalCode, request.IsActive, cancellationToken);
        await _invalidation.InvalidateLocationAsync(request.CountryCode, cancellationToken);
        return result;
    }
}
