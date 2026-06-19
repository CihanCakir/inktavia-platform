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
    public Guid? FileId { get; private set; }
    public string? OriginalFileNameSnapshot { get; private set; }
    public string? ContentTypeSnapshot { get; private set; }
    public long? SizeInBytesSnapshot { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public VesselDocumentStatus DocumentStatus { get; private set; }
    public string? Notes { get; private set; }
    public string? DocumentCategory { get; private set; }
    public string? IssuingAuthority { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public long? ApprovedByUserId { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselDocumentEntity() { }

    public static VesselDocumentEntity Create(
        long vesselId,
        string documentTypeCode,
        string documentName,
        Guid? fileId,
        string? originalFileNameSnapshot,
        string? contentTypeSnapshot,
        long? sizeInBytesSnapshot,
        DateTime? expiresAt,
        string? notes)
    {
        return new VesselDocumentEntity
        {
            VesselId = vesselId,
            DocumentTypeCode = documentTypeCode.ToUpperInvariant(),
            DocumentName = documentName.Trim(),
            FileId = fileId,
            OriginalFileNameSnapshot = originalFileNameSnapshot,
            ContentTypeSnapshot = contentTypeSnapshot,
            SizeInBytesSnapshot = sizeInBytesSnapshot,
            ExpiresAt = expiresAt,
            DocumentStatus = VesselDocumentStatus.Active,
            Notes = notes,
            IsActive = true
        };
    }

    public void Update(
        string documentName,
        string documentTypeCode,
        Guid? fileId,
        string? originalFileNameSnapshot,
        string? contentTypeSnapshot,
        long? sizeInBytesSnapshot,
        DateTime? expiresAt,
        string? notes)
    {
        DocumentName = documentName.Trim();
        DocumentTypeCode = documentTypeCode.ToUpperInvariant();
        FileId = fileId;
        OriginalFileNameSnapshot = originalFileNameSnapshot;
        ContentTypeSnapshot = contentTypeSnapshot;
        SizeInBytesSnapshot = sizeInBytesSnapshot;
        ExpiresAt = expiresAt;
        Notes = notes;
    }

    public void ChangeStatus(VesselDocumentStatus status) => DocumentStatus = status;
    public void Deactivate() => IsActive = false;

    public void Approve(long approvedByUserId)
    {
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = approvedByUserId;
    }
}
