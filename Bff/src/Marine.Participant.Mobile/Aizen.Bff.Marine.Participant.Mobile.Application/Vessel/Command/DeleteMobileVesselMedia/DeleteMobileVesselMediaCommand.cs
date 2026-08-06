using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>DELETE /api/v1/mobile/vessels/{id}/media/{mediaId} — remove one of the caller's vessel photos.
/// Owner + media-membership gated (clean not-found). Returns the deleted media id.</summary>
public sealed class DeleteMobileVesselMediaCommand : AizenCommand<MobileVesselMediaDeletedDto>
{
    public DeleteMobileVesselMediaCommand(long vesselId, long mediaId)
    {
        VesselId = vesselId;
        MediaId = mediaId;
    }

    public long VesselId { get; }

    public long MediaId { get; }
}
