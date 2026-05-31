using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class ActivateMeasurementUnitCommand : AizenCommand<bool>
{
    public long Id { get; }

    public ActivateMeasurementUnitCommand(long id)
    {
        Id = id;
    }
}
