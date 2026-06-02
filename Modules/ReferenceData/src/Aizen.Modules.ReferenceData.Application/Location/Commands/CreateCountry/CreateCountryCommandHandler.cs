using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCountryCommandHandler : AizenCommandHandler<CreateCountryCommand, CountryDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateCountryCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CountryDto?> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateCountryAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateLocationAsync(cancellationToken: cancellationToken);
        return result;
    }
}
