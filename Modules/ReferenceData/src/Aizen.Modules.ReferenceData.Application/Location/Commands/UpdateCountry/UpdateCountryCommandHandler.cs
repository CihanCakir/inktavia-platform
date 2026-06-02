using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateCountryCommandHandler : AizenCommandHandler<UpdateCountryCommand, CountryDto>
{
    private readonly ILocationReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateCountryCommandHandler(ILocationReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<CountryDto?> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateCountryAsync(request.CountryCode, request.DefaultCurrencyCode, request.PhoneCode, request.IsActive, cancellationToken);
        await _invalidation.InvalidateLocationAsync(request.CountryCode, cancellationToken);
        return result;
    }
}
