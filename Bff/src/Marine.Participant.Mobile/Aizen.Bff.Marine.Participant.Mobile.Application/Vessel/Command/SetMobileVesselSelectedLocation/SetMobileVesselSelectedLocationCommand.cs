using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>PUT /api/v1/mobile/vessels/{id}/location/selected — set (or clear) the owner's explicit location choice
/// on a vessel owned by the authenticated participant. A marina pick is resolved to name/coords before the write.</summary>
public sealed class SetMobileVesselSelectedLocationCommand : AizenCommand<MobileSelectedLocationDto>
{
    public SetMobileVesselSelectedLocationCommand(long vesselId, SetMobileVesselSelectedLocationRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }
    public SetMobileVesselSelectedLocationRequest Request { get; }
}
