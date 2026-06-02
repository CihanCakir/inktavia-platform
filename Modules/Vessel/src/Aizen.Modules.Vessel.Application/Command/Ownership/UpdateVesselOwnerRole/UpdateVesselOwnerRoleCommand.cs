using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;

namespace Aizen.Modules.Vessel.Application.Command.Ownership;

[DocumentationInfo("Update Vessel Owner Role Command", "Carries the payload required to change a vessel owner's role.")]
public sealed class UpdateVesselOwnerRoleCommand : AizenCommand<VesselOwnerDto>
{
    public long VesselId { get; }
    public long OwnerId { get; }
    public UpdateVesselOwnerRoleRequest Request { get; }

    public UpdateVesselOwnerRoleCommand(long vesselId, long ownerId, UpdateVesselOwnerRoleRequest request)
    {
        VesselId = vesselId;
        OwnerId = ownerId;
        Request = request;
    }
}
