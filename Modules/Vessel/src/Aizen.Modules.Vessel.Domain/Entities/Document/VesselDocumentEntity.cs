using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel document entity", "Official document or certificate linked to a vessel.")]
public sealed class VesselDocumentEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public string DocumentTypeCode { get; private set; } = default!;
    public string DocumentName { get; private set; } = default!;
    public string? FileId { get; private set; }
    public string? FileName { get; private set; }
    public string? FileUrl { get; private set; }
    public string? MimeType { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public VesselDocumentStatus DocumentStatus { get; private set; }
    public string? Notes { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselDocumentEntity() { }

    public static VesselDocumentEntity Create(
        long vesselId, string documentTypeCode, string documentName,
        string? fileId, string? fileName, string? fileUrl, string? mimeType,
        DateTime? expiresAt, string? notes)
    {
        return new VesselDocumentEntity
        {
            VesselId = vesselId,
            DocumentTypeCode = documentTypeCode.ToUpperInvariant(),
            DocumentName = documentName.Trim(),
            FileId = fileId,
            FileName = fileName,
            FileUrl = fileUrl,
            MimeType = mimeType,
            ExpiresAt = expiresAt,
            DocumentStatus = VesselDocumentStatus.Active,
            Notes = notes,
            IsActive = true
        };
    }

    public void Update(string documentName, DateTime? expiresAt, string? notes)
    {
        DocumentName = documentName.Trim();
        ExpiresAt = expiresAt;
        Notes = notes;
    }

    public void ChangeStatus(VesselDocumentStatus status) => DocumentStatus = status;
    public void Deactivate() => IsActive = false;
}
