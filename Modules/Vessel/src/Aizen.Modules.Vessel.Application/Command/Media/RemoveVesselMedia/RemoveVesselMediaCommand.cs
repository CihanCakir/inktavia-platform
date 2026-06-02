using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Remove Vessel Media Command", "Carries the payload required to deactivate a vessel media item.")]
public sealed class RemoveVesselMediaCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public long RequestingUserId { get; }

    public RemoveVesselMediaCommand(long vesselId, long mediaId, long requestingUserId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        RequestingUserId = requestingUserId;
    }
}
