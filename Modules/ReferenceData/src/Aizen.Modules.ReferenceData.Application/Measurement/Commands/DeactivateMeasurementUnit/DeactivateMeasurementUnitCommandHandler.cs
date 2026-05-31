using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class DeactivateMeasurementUnitCommandHandler : AizenCommandHandler<DeactivateMeasurementUnitCommand, bool>
{
    private readonly IMeasurementReferenceService _service;

    public DeactivateMeasurementUnitCommandHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(DeactivateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return true;
    }
}
