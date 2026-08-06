using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/media — attach an already-uploaded (client-side presigned + completed)
/// file to the vessel as a photo. Owner-gated; returns the created media (with a presigned read URL).</summary>
public sealed class AttachMobileVesselMediaCommand : AizenCommand<MobileVesselMediaDto>
{
    public AttachMobileVesselMediaCommand(long vesselId, AttachMobileVesselMediaRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }

    public AttachMobileVesselMediaRequest Request { get; }
}
