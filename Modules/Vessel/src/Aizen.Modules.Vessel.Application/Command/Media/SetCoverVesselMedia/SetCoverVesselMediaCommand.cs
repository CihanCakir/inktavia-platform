using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Set Cover Vessel Media Command", "Carries the payload required to designate a media item as the vessel cover.")]
public sealed class SetCoverVesselMediaCommand : AizenCommand<bool>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public long RequestingUserId { get; }

    public SetCoverVesselMediaCommand(long vesselId, long mediaId, long requestingUserId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        RequestingUserId = requestingUserId;
    }
}
