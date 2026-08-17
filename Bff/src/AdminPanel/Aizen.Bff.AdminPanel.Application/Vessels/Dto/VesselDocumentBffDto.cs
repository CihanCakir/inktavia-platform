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
    /// <summary>Presigned GET URL for the document file (R5). Empty when the file could not be presigned.</summary>
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    /// <summary>Display name of the approving admin (resolved via Identity); null if unresolved.</summary>
    public string? ApprovedByName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    /// <summary>
    /// Version history. The Vessel module does not track document versions today, so this is always empty — but the
    /// field MUST be present (the admin-web slide panel reads <c>versions.length</c>/<c>versions.map</c> unguarded).
    /// </summary>
    public List<VesselDocumentVersionBffDto> Versions { get; set; } = new();
}

[DocumentationInfo("Vessel document version BFF DTO", "One version entry for a vessel document (reserved — no version tracking in the module yet).")]
public sealed class VesselDocumentVersionBffDto
{
    public string VersionId { get; set; } = default!;
    public int VersionNumber { get; set; }
    public string? UploadedAt { get; set; }
    public string? UploadedByName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Notes { get; set; }
    public bool IsCurrent { get; set; }
}
