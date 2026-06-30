using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Set Cover Vessel Media Command", "Carries the payload required to designate a media item as the vessel cover.")]
public sealed class SetCoverVesselMediaCommand : AizenCommand<SetCoverVesselMediaResponse>
{
    public long VesselId { get; }
    public long MediaId { get; }

    public SetCoverVesselMediaCommand(long vesselId, long mediaId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
    }
}
