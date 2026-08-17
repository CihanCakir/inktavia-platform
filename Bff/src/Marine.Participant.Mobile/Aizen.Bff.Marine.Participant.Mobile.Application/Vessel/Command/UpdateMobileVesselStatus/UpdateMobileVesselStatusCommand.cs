using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>PUT /api/v1/mobile/vessels/{id}/status — change the operational status of one of the authenticated
/// participant's vessels (user-settable states: Active / Passive / UnderMaintenance). Ownership-gated (clean
/// not-found for a foreign/unknown id); the module enforces the valid-transition graph. Returns the re-read detail.</summary>
public sealed class UpdateMobileVesselStatusCommand : AizenCommand<MobileVesselDetailDto>
{
    public UpdateMobileVesselStatusCommand(long vesselId, UpdateMobileVesselStatusRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }

    public UpdateMobileVesselStatusRequest Request { get; }
}
