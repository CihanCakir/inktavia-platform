using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>PUT /api/v1/mobile/vessels/{id}/location/current — record the auto-detected device position as a new
/// current-location snapshot on a vessel owned by the authenticated participant.</summary>
public sealed class UpdateMobileVesselCurrentLocationCommand : AizenCommand<MobileVesselLocationDto>
{
    public UpdateMobileVesselCurrentLocationCommand(long vesselId, UpdateMobileVesselCurrentLocationRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }
    public UpdateMobileVesselCurrentLocationRequest Request { get; }
}
