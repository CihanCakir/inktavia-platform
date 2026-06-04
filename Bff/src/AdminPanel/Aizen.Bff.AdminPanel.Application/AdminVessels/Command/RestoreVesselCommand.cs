using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class RestoreVesselCommand : AizenCommand<RestoreVesselResponse>
{
    public long VesselId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public RestoreVesselCommand(long vesselId, string authorization, string userToken)
    {
        VesselId = vesselId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
