using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Remove Vessel Owner Command", "Carries the payload required to remove a vessel owner.")]
public sealed class RemoveVesselOwnerCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long OwnerId { get; }
    public long RequestingUserId { get; }

    public RemoveVesselOwnerCommand(long vesselId, long ownerId, long requestingUserId)
    {
        VesselId = vesselId;
        OwnerId = ownerId;
        RequestingUserId = requestingUserId;
    }
}
