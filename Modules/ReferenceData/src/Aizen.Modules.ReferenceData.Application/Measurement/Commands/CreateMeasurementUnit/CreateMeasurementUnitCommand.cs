using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class CreateMeasurementUnitCommand : AizenCommand<MeasurementUnitDto>
{
    public CreateMeasurementUnitRequest Request { get; }

    public CreateMeasurementUnitCommand(CreateMeasurementUnitRequest request)
    {
        Request = request;
    }
}
