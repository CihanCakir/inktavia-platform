using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Document;

[DocumentationInfo("Vessel document DTO", "Represents a vessel-related official document such as registration or insurance.")]
public sealed class VesselDocumentDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string DocumentTypeCode { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public Guid? FileId { get; set; }
    public string? OriginalFileNameSnapshot { get; set; }
    public string? ContentTypeSnapshot { get; set; }
    public long? SizeInBytesSnapshot { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public VesselDocumentStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? AccessUrlExpiresAt { get; set; }
}

