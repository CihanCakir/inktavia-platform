using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

public sealed class UpdateVesselBffCommand : AizenCommand<UpdateVesselResponse>
{
    public long VesselId { get; }
    public UpdateVesselRequest Request { get; }
    public UpdateVesselBffCommand(long vesselId, UpdateVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
