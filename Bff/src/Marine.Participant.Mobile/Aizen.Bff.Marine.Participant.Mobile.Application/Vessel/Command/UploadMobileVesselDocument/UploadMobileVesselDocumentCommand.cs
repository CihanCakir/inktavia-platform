using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/documents — attach an already-uploaded (client-side presigned +
/// completed) file to the vessel with its DOCUMENT_TYPE. Owner-gated; returns the created document (with a
/// presigned read URL). The bytes were uploaded directly to storage via /mobile/uploads — never through the BFF.</summary>
public sealed class UploadMobileVesselDocumentCommand : AizenCommand<MobileVesselDocumentDto>
{
    public long VesselId { get; set; }
    public Guid FileId { get; set; }
    public string DocumentTypeCode { get; set; } = default!;
    public string? DocumentName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}
