using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>GET /api/v1/mobile/vessels/{id} — full detail, gated to the caller's own vessels.</summary>
public sealed class GetMobileVesselDetailQuery : AizenQuery<MobileVesselDetailDto>
{
    public long VesselId { get; }
    public GetMobileVesselDetailQuery(long vesselId) => VesselId = vesselId;
}
