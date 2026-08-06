using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>GET /api/v1/mobile/vessels/{id}/media — the caller's vessel photos (owner-gated; each with a fresh
/// presigned read URL, cover first). A foreign/unknown vessel id yields a clean not-found.</summary>
public sealed class GetMobileVesselMediaQuery : AizenQuery<List<MobileVesselMediaDto>>
{
    public GetMobileVesselMediaQuery(long vesselId) => VesselId = vesselId;

    public long VesselId { get; }
}
