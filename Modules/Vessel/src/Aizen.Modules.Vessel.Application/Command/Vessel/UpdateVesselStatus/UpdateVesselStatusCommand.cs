using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Status Command", "Carries the payload required to change a vessel's operational status.")]
public sealed class UpdateVesselStatusCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public UpdateVesselStatusRequest Request { get; }
    public long RequestingUserId { get; }

    public UpdateVesselStatusCommand(long vesselId, UpdateVesselStatusRequest request, long requestingUserId)
    {
        VesselId = vesselId;
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
