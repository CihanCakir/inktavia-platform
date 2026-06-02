using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Engine;

[DocumentationInfo("Remove Vessel Engine Command", "Carries the payload required to deactivate a vessel engine.")]
public sealed class RemoveVesselEngineCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long EngineId { get; }
    public long RequestingUserId { get; }

    public RemoveVesselEngineCommand(long vesselId, long engineId, long requestingUserId)
    {
        VesselId = vesselId;
        EngineId = engineId;
        RequestingUserId = requestingUserId;
    }
}
