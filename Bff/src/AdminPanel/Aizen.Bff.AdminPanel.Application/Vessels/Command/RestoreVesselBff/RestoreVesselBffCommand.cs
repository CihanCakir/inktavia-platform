using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

public sealed class RestoreVesselBffCommand : AizenCommand<RestoreVesselResponse>
{
    public long VesselId { get; }
    public RestoreVesselBffCommand(long vesselId)
    {
        VesselId = vesselId;
    }
}
