using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class DeactivateMeasurementUnitCommandHandler : AizenCommandHandler<DeactivateMeasurementUnitCommand, bool>
{
    private readonly IMeasurementReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public DeactivateMeasurementUnitCommandHandler(IMeasurementReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(DeactivateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        await _invalidation.InvalidateMeasurementUnitAsync(request.Id, cancellationToken);
        return true;
    }
}
