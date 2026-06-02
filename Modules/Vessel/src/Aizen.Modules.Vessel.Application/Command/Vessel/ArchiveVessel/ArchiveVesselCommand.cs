using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Archive Vessel Command", "Carries the payload required to archive a vessel.")]
public sealed class ArchiveVesselCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Request { get; }

    public ArchiveVesselCommand(long vesselId, ArchiveVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
