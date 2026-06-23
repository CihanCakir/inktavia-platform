namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Profile approval checklist BFF DTO", "Computed checklist flags for organizer/venue approval review screen.")]
public sealed class ProfileApprovalChecklistBffDto
{
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public bool CompanyNameProvided { get; set; }
    public bool TaxNumberProvided { get; set; }
    public bool DocumentsUploaded { get; set; }
    public bool DuplicateAccountFound { get; set; }
    public bool SuspiciousActivityFound { get; set; }
}

[DocumentationInfo("Profile approval document BFF DTO", "Document metadata for review screen.")]
public sealed class ProfileApprovalDocumentBffDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string FileId { get; set; } = null!;
    public string UploadedAt { get; set; } = null!;
    public string? Format { get; set; }
    public string? Size { get; set; }
    public string? Issuer { get; set; }
    public string? MatchScore { get; set; }
}

[DocumentationInfo("Profile approval risk signal BFF DTO", "Risk flag entry for the review screen risk panel.")]
public sealed class ProfileApprovalRiskSignalBffDto
{
    public string Level { get; set; } = null!;       // "high" | "medium" | "low"
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
}

[DocumentationInfo("Profile approval activity item BFF DTO", "Single activity log entry for the review screen timeline.")]
public sealed class ProfileApprovalActivityItemBffDto
{
    public string EventType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? PerformedBy { get; set; }
    public string PerformedAt { get; set; } = null!;
}
