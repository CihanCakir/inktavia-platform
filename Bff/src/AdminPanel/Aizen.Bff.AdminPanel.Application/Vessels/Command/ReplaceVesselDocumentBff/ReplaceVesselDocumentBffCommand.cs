using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Replace vessel document BFF command", "Completes the upload session then swaps an existing document's file for the newly uploaded FileId (B4 — new version).")]
public sealed class ReplaceVesselDocumentBffCommand : AizenCommand<UpdateVesselDocumentResponse>
{
    public long VesselId { get; }
    public long DocumentId { get; }
    public string FileId { get; }               // FileStorage FileId (Guid as string)
    public string UploadSessionCode { get; }
    public string DocumentTypeCode { get; }
    public string DocumentName { get; }
    public DateTime? ExpiresAt { get; }
    public string? Notes { get; }

    public ReplaceVesselDocumentBffCommand(
        long vesselId, long documentId, string fileId, string uploadSessionCode,
        string documentTypeCode, string documentName, DateTime? expiresAt, string? notes)
    {
        VesselId = vesselId;
        DocumentId = documentId;
        FileId = fileId;
        UploadSessionCode = uploadSessionCode;
        DocumentTypeCode = documentTypeCode;
        DocumentName = documentName;
        ExpiresAt = expiresAt;
        Notes = notes;
    }
}
