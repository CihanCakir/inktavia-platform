using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Add Vessel Engine Command", "Carries the payload required to add a new engine to a vessel.")]
public sealed class AddVesselEngineCommand : AizenCommand<AddVesselEngineResponse>
{
    public long VesselId { get; }
    public AddVesselEngineRequest Request { get; }

    public AddVesselEngineCommand(long vesselId, AddVesselEngineRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }
}
