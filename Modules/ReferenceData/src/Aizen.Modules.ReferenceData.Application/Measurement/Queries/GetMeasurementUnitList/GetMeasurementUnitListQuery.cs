using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitListQuery : AizenQuery<IReadOnlyList<MeasurementUnitDto>>
{
    public bool OnlyActive { get; }

    public GetMeasurementUnitListQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
