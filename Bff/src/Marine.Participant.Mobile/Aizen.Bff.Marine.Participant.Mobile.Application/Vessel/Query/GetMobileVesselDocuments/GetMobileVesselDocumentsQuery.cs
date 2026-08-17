using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>GET /api/v1/mobile/vessels/{id}/documents — the caller's vessel documents (owner-gated; each carries
/// a freshly-resolved presigned read URL). A foreign/unknown vessel id yields a clean not-found.</summary>
public sealed class GetMobileVesselDocumentsQuery : AizenQuery<List<MobileVesselDocumentDto>>
{
    public GetMobileVesselDocumentsQuery(long vesselId) => VesselId = vesselId;

    public long VesselId { get; }
}
