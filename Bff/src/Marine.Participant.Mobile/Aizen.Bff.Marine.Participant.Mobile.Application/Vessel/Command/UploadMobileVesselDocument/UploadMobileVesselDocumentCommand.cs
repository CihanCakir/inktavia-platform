using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/documents — the raw file bytes + document metadata. Server-side
/// uploads to FileStorage (M3c pattern), attaches the file to the vessel with its DOCUMENT_TYPE, and returns the
/// created document (with a presigned read URL). Owner-gated.</summary>
public sealed class UploadMobileVesselDocumentCommand : AizenCommand<MobileVesselDocumentDto>
{
    public long VesselId { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "document";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeInBytes { get; set; }

    public string DocumentTypeCode { get; set; } = default!;
    public string? DocumentName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}
