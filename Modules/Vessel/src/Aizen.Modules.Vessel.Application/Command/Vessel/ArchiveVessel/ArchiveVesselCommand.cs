using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Archive Vessel Command", "Carries the payload required to archive a vessel.")]
public sealed class ArchiveVesselCommand : AizenCommand<ArchiveVesselResponse>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Request { get; }

    public ArchiveVesselCommand(long vesselId, ArchiveVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
