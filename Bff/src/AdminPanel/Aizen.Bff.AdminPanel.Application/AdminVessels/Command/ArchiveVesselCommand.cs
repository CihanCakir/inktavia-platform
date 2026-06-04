using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class ArchiveVesselCommand : AizenCommand<ArchiveVesselResponse>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Payload { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public ArchiveVesselCommand(long vesselId, ArchiveVesselRequest payload, string authorization, string userToken)
    {
        VesselId = vesselId;
        Payload = payload;
        Authorization = authorization;
        UserToken = userToken;
    }
}
