using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

public sealed class ArchiveVesselBffCommand : AizenCommand<ArchiveVesselResponse>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Payload { get; }
    public ArchiveVesselBffCommand(long vesselId, ArchiveVesselRequest payload)
    {
        VesselId = vesselId;
        Payload = payload;
    }
}
