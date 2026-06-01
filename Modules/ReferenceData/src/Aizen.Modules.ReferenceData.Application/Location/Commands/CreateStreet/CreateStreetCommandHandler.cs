using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateStreetCommandHandler : AizenCommandHandler<CreateStreetCommand, StreetDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateStreetCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<StreetDto?> Handle(CreateStreetCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateStreetAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.NeighborhoodCode, request.StreetCode, request.Name, request.PostalCode, cancellationToken);
        await _invalidation.InvalidateNeighborhoodAsync(request.CountryCode, request.CityCode, request.DistrictCode, cancellationToken);
        return result;
    }
}
