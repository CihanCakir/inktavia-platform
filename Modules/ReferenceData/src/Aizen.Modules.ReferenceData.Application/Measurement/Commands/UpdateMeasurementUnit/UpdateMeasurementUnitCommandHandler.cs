using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class UpdateMeasurementUnitCommandHandler : AizenCommandHandler<UpdateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly IMeasurementReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateMeasurementUnitCommandHandler(IMeasurementReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<MeasurementUnitDto?> Handle(UpdateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request.Id, request.Name, request.Symbol, request.ConversionFactorToBase, request.BaseUnitCode, request.IsActive, cancellationToken);
        await _invalidation.InvalidateMeasurementUnitAsync(request.Id, cancellationToken: cancellationToken);
        return result;
    }
}
