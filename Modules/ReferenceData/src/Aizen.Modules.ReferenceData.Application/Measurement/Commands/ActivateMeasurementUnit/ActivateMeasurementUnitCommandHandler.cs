using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class ActivateMeasurementUnitCommandHandler : AizenCommandHandler<ActivateMeasurementUnitCommand, bool>
{
    private readonly IMeasurementReferenceService _service;

    public ActivateMeasurementUnitCommandHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(ActivateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return true;
    }
}
