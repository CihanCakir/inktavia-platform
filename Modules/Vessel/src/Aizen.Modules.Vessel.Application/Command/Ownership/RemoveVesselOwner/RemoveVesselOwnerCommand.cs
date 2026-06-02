using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Remove Vessel Owner Command", "Carries the payload required to remove a vessel owner.")]
public sealed class RemoveVesselOwnerCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long OwnerId { get; }

    public RemoveVesselOwnerCommand(long vesselId, long ownerId)
    {
        VesselId = vesselId;
        OwnerId = ownerId;
    }
}
