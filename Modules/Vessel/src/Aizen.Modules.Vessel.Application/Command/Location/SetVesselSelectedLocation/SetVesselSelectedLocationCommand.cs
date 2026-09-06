using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Location;
using Aizen.Modules.Vessel.Abstraction.Response.Location;

namespace Aizen.Modules.Vessel.Application.Command.Location;

[DocumentationInfo("Set Vessel Selected Location Command", "Carries the owner's explicit location choice for a vessel.")]
public sealed class SetVesselSelectedLocationCommand : AizenCommand<SetVesselSelectedLocationResponse>
{
    public long VesselId { get; }
    public SetVesselSelectedLocationRequest Request { get; }

    public SetVesselSelectedLocationCommand(long vesselId, SetVesselSelectedLocationRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
