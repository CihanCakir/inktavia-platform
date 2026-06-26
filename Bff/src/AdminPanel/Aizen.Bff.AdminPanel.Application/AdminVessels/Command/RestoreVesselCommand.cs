using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class RestoreVesselCommand : AizenCommand<RestoreVesselResponse>
{
    public long VesselId { get; }
    public RestoreVesselCommand(long vesselId)
    {
        VesselId = vesselId;
    }
}
