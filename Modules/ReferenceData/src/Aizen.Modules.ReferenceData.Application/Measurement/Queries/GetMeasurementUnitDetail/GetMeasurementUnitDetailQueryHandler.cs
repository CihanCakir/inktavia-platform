using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitDetailQueryHandler : AizenQueryHandler<GetMeasurementUnitDetailQuery, MeasurementUnitDto?>
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitDetailQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<MeasurementUnitDto?> Handle(GetMeasurementUnitDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
