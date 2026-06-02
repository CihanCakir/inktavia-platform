using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Update Vessel Engine Command", "Carries the payload required to update an existing vessel engine.")]
public sealed class UpdateVesselEngineCommand : AizenCommand<VesselEngineDto>
{
    public long VesselId { get; }
    public long EngineId { get; }
    public UpdateVesselEngineRequest Request { get; }

    public UpdateVesselEngineCommand(long vesselId, long engineId, UpdateVesselEngineRequest request)
    {
        VesselId = vesselId;
        EngineId = engineId;
        Request = request;
    }
}
