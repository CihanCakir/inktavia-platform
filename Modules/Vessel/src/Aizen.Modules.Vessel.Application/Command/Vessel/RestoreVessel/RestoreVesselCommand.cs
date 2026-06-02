using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Restore Vessel Command", "Carries the payload required to restore an archived vessel.")]
public sealed class RestoreVesselCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long RequestingUserId { get; }

    public RestoreVesselCommand(long vesselId, long requestingUserId)
    {
        VesselId = vesselId;
        RequestingUserId = requestingUserId;
    }
}
