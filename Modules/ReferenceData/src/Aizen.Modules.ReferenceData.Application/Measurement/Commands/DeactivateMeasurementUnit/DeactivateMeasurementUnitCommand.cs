using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Commands;

public sealed class DeactivateMeasurementUnitCommand : AizenCommand<bool>
{
    public long Id { get; }

    public DeactivateMeasurementUnitCommand(long id)
    {
        Id = id;
    }
}
