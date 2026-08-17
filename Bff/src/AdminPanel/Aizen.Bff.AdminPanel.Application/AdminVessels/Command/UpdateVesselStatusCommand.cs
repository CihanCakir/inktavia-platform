using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class UpdateVesselStatusCommand : AizenCommand<UpdateVesselStatusResponse>
{
    public long VesselId { get; }
    public UpdateVesselStatusRequest Payload { get; }
    public UpdateVesselStatusCommand(long vesselId, UpdateVesselStatusRequest payload)
    {
        VesselId = vesselId;
        Payload = payload;
    }
}
