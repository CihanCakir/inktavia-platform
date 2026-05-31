using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class UpdateMeasurementUnitCommandHandler : AizenCommandHandler<UpdateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly IMeasurementReferenceService _service;

    public UpdateMeasurementUnitCommandHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<MeasurementUnitDto?> Handle(UpdateMeasurementUnitCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateAsync(request.Id, request.Name, request.Symbol, request.ConversionFactorToBase, request.BaseUnitCode, request.IsActive, cancellationToken);
    }
}
