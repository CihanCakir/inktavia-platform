using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class UpdateVesselCommand : AizenCommand<UpdateVesselResponse>
{
    public long VesselId { get; }
    public UpdateVesselRequest Request { get; }
    public string UserToken { get; }
    public UpdateVesselCommand(long vesselId, UpdateVesselRequest request, string userToken)
    {
        VesselId = vesselId;
        Request = request;
        UserToken = userToken;
    }
}
