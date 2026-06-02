using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCityCommandHandler : AizenCommandHandler<CreateCityCommand, CityDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateCityCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CityDto?> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateCityAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateLocationAsync(request.Request.CountryCode, cancellationToken);
        return result;
    }
}
