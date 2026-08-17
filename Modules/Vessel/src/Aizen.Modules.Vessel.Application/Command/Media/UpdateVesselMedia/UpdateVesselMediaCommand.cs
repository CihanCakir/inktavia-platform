using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Update Vessel Media Command", "Carries the payload required to update a vessel media item's metadata.")]
public sealed class UpdateVesselMediaCommand : AizenCommand<UpdateVesselMediaResponse>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public UpdateVesselMediaRequest Request { get; }

    public UpdateVesselMediaCommand(long vesselId, long mediaId, UpdateVesselMediaRequest request)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        Request = request;
    }
}
