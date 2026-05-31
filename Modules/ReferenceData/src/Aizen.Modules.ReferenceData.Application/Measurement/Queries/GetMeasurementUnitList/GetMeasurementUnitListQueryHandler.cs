using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitListQueryHandler : AizenQueryHandler<GetMeasurementUnitListQuery, IReadOnlyList<MeasurementUnitDto>>
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitListQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<MeasurementUnitDto>> Handle(GetMeasurementUnitListQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetListAsync(request.OnlyActive, cancellationToken);
    }
}
