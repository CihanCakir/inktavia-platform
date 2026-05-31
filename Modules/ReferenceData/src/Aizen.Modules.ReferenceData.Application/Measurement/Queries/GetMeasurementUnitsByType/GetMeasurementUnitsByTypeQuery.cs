using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitsByTypeQuery : AizenQuery<IReadOnlyList<MeasurementUnitDto>>
{
    public MeasurementUnitType UnitType { get; }
    public bool OnlyActive { get; }

    public GetMeasurementUnitsByTypeQuery(MeasurementUnitType unitType, bool onlyActive = true)
    {
        UnitType = unitType;
        OnlyActive = onlyActive;
    }
}
