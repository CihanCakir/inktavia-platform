using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class ArchiveVesselCommand : AizenCommand<ArchiveVesselResponse>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Payload { get; }
    public ArchiveVesselCommand(long vesselId, ArchiveVesselRequest payload)
    {
        VesselId = vesselId;
        Payload = payload;
    }
}
