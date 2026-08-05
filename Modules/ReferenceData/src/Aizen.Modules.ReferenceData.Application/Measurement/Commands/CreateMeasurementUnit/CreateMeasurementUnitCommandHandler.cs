using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class CreateMeasurementUnitCommandHandler : AizenCommandHandler<CreateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly IMeasurementReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateMeasurementUnitCommandHandler(IMeasurementReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<MeasurementUnitDto?> Handle(CreateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.Request, cancellationToken);
        await _invalidation.InvalidateMeasurementUnitAsync(code: result.Code, cancellationToken: cancellationToken);
        return result;
    }
}
