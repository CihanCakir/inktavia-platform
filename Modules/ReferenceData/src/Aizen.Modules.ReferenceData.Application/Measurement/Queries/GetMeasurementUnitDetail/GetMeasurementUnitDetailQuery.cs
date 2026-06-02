using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

public sealed class GetMeasurementUnitDetailQuery : AizenQuery<MeasurementUnitDto?>
{
    public long Id { get; }

    public GetMeasurementUnitDetailQuery(long id)
    {
        Id = id;
    }
}
