using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/restore — restore one of the authenticated participant's archived
/// vessels back to the active list. Ownership-gated (clean not-found for a foreign/unknown id). Returns the
/// re-read detail (IsArchived = false); the vessel reappears in the active list/Home/picker.</summary>
public sealed class RestoreMobileVesselCommand : AizenCommand<MobileVesselDetailDto>
{
    public RestoreMobileVesselCommand(long vesselId) => VesselId = vesselId;

    public long VesselId { get; }
}
