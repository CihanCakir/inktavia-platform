using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Media;

namespace Aizen.Modules.Vessel.Application.Command.Media;

[DocumentationInfo("Update Vessel Media Command", "Carries the payload required to update a vessel media item's metadata.")]
public sealed class UpdateVesselMediaCommand : AizenCommand<VesselMediaDto>
{
    public long VesselId { get; }
    public long MediaId { get; }
    public UpdateVesselMediaRequest Request { get; }
    public long RequestingUserId { get; }

    public UpdateVesselMediaCommand(long vesselId, long mediaId, UpdateVesselMediaRequest request, long requestingUserId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
        Request = request;
        RequestingUserId = requestingUserId;
    }
}
