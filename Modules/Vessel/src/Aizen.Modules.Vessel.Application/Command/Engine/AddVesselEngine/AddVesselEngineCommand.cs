using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Add Vessel Engine Command", "Carries the payload required to add a new engine to a vessel.")]
public sealed class AddVesselEngineCommand : AizenCommand<VesselEngineDto>
{
    public long VesselId { get; }
    public AddVesselEngineRequest Request { get; }
    public long RequestingUserId { get; }

    public AddVesselEngineCommand(long vesselId, AddVesselEngineRequest request, long requestingUserId)
    {
        VesselId = vesselId;
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
