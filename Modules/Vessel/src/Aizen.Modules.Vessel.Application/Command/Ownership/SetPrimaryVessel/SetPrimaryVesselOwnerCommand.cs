using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Set Primary Vessel Owner Command", "Carries the payload required to designate a vessel owner as primary.")]
public sealed class SetPrimaryVesselOwnerCommand : AizenCommand<SetPrimaryVesselOwnerResponse>
{
    public long VesselId { get; }
    public long OwnerId { get; }

    public SetPrimaryVesselOwnerCommand(long vesselId, long ownerId)
    {
        VesselId = vesselId;
        OwnerId = ownerId;
    }
}
