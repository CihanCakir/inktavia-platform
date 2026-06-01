using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class ActivateMeasurementUnitCommandHandler : AizenCommandHandler<ActivateMeasurementUnitCommand, bool>
{
    private readonly IMeasurementReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public ActivateMeasurementUnitCommandHandler(IMeasurementReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(ActivateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateMeasurementUnitAsync(request.Id, cancellationToken);
        return true;
    }
}
