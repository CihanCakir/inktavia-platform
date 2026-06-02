using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Restore Vessel Command", "Carries the payload required to restore an archived vessel.")]
public sealed class RestoreVesselCommand : AizenCommand<bool>
{
    public long VesselId { get; }

    public RestoreVesselCommand(long vesselId)
    {
        VesselId = vesselId;
    }
}
