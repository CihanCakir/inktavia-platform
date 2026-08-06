using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Vessel document BFF DTO", "Vessel document with computed status fields for the Documents tab.")]
public sealed class VesselDocumentBffDto
{
    public long Id { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentCategory { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? IssuingAuthority { get; set; }
    public string? DocumentStatus { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
