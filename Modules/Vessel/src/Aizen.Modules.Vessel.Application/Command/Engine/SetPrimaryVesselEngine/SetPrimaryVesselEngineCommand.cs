using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Set Primary Vessel Engine Command", "Carries the payload required to designate an engine as primary for a vessel.")]
public sealed class SetPrimaryVesselEngineCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long EngineId { get; }
    public long RequestingUserId { get; }

    public SetPrimaryVesselEngineCommand(long vesselId, long engineId, long requestingUserId)
    {
        VesselId = vesselId;
        EngineId = engineId;
        RequestingUserId = requestingUserId;
    }
}
