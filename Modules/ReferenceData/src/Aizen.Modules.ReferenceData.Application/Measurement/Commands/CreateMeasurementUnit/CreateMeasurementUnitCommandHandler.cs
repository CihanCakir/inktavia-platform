using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class CreateMeasurementUnitCommandHandler : AizenCommandHandler<CreateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly IMeasurementReferenceService _service;

    public CreateMeasurementUnitCommandHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<MeasurementUnitDto?> Handle(CreateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateAsync(request.Request, cancellationToken);
    }
}
