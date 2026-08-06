using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/media/{mediaId}/cover — make a photo the vessel's cover. Owner +
/// media-membership gated. Returns the re-read gallery (new cover flagged); the module invalidates the media,
/// detail and user-list caches so the list/Home cover updates immediately.</summary>
public sealed class SetCoverMobileVesselMediaCommand : AizenCommand<List<MobileVesselMediaDto>>
{
    public SetCoverMobileVesselMediaCommand(long vesselId, long mediaId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
    }

    public long VesselId { get; }

    public long MediaId { get; }
}
