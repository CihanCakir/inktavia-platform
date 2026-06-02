using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Set Primary Vessel Owner Command", "Carries the payload required to designate a vessel owner as primary.")]
public sealed class SetPrimaryVesselOwnerCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long OwnerId { get; }
    public long RequestingUserId { get; }

    public SetPrimaryVesselOwnerCommand(long vesselId, long ownerId, long requestingUserId)
    {
        VesselId = vesselId;
        OwnerId = ownerId;
        RequestingUserId = requestingUserId;
    }
}
