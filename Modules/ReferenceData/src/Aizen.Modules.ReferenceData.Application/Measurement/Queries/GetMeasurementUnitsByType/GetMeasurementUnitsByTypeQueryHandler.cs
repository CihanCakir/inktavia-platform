using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitsByTypeQueryHandler : AizenQueryHandler<GetMeasurementUnitsByTypeQuery, IReadOnlyList<MeasurementUnitDto>>
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitsByTypeQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<MeasurementUnitDto>> Handle(GetMeasurementUnitsByTypeQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByTypeAsync(request.UnitType, request.OnlyActive, cancellationToken);
    }
}
