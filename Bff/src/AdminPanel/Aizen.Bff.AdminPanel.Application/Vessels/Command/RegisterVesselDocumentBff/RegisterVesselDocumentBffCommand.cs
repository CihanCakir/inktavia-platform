using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Register vessel document BFF command", "Completes the upload session then registers a new vessel document with the uploaded FileId (B3).")]
public sealed class RegisterVesselDocumentBffCommand : AizenCommand<AddVesselDocumentResponse>
{
    public long VesselId { get; }
    public string FileId { get; }               // FileStorage FileId (Guid as string)
    public string UploadSessionCode { get; }
    public string DocumentTypeCode { get; }
    public string DocumentName { get; }
    public DateTime? ExpiresAt { get; }
    public string? Notes { get; }

    public RegisterVesselDocumentBffCommand(
        long vesselId, string fileId, string uploadSessionCode,
        string documentTypeCode, string documentName, DateTime? expiresAt, string? notes)
    {
        VesselId = vesselId;
        FileId = fileId;
        UploadSessionCode = uploadSessionCode;
        DocumentTypeCode = documentTypeCode;
        DocumentName = documentName;
        ExpiresAt = expiresAt;
        Notes = notes;
    }
}
