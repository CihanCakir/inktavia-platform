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
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? MimeType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public VesselDocumentStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
