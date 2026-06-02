using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Set Primary Vessel Engine Command", "Carries the payload required to designate an engine as primary for a vessel.")]
public sealed class SetPrimaryVesselEngineCommand : AizenCommand<SetPrimaryVesselEngineResponse>
{
    public long VesselId { get; }
    public long EngineId { get; }

    public SetPrimaryVesselEngineCommand(long vesselId, long engineId)
    {
        VesselId = vesselId;
        EngineId = engineId;
    }
}
