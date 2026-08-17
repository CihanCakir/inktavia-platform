using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>DELETE /api/v1/mobile/vessels/{id}/documents/{docId} — remove one of the caller's vessel documents.
/// Owner-gated (clean not-found for a foreign/unknown vessel/doc id). Returns the deleted document id.</summary>
public sealed class DeleteMobileVesselDocumentCommand : AizenCommand<MobileVesselDocumentDeletedDto>
{
    public DeleteMobileVesselDocumentCommand(long vesselId, long documentId)
    {
        VesselId = vesselId;
        DocumentId = documentId;
    }

    public long VesselId { get; }

    public long DocumentId { get; }
}
