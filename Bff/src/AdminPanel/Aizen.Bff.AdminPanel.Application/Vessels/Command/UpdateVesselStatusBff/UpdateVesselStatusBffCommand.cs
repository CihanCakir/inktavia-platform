using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

public sealed class UpdateVesselStatusBffCommand : AizenCommand<UpdateVesselStatusResponse>
{
    public long VesselId { get; }
    public UpdateVesselStatusRequest Payload { get; }
    public UpdateVesselStatusBffCommand(long vesselId, UpdateVesselStatusRequest payload)
    {
        VesselId = vesselId;
        Payload = payload;
    }
}
