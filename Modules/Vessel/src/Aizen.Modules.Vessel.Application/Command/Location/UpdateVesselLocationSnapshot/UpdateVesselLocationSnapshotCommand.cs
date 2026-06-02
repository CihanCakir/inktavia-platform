using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Location;
using Aizen.Modules.Vessel.Abstraction.Response.Location;

namespace Aizen.Modules.Vessel.Application.Command.Location;

[DocumentationInfo("Update Vessel Location Snapshot Command", "Carries the payload required to record a vessel's current location.")]
public sealed class UpdateVesselLocationSnapshotCommand : AizenCommand<UpdateVesselLocationSnapshotResponse>
{
    public long VesselId { get; }
    public UpdateVesselLocationSnapshotRequest Request { get; }

    public UpdateVesselLocationSnapshotCommand(long vesselId, UpdateVesselLocationSnapshotRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
