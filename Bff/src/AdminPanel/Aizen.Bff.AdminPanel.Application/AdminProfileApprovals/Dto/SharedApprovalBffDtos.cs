using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

// ── Provider Onboarding BFF DTOs ──────────────────────────────────────────────

[DocumentationInfo("Provider onboarding BFF DTO", "Onboarding wizard state for the admin approval review screen. Includes step statuses, revision info, and submitted documents.")]
public sealed class ProviderOnboardingBffDto
{
    /// <summary>Overall onboarding status: NotStarted | InProgress | Submitted | NeedsRevision | Completed</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Per-step status map keyed by camelCase step name (e.g. "businessIdentity", "serviceCapabilities").</summary>
    public Dictionary<string, string> StepStatuses { get; set; } = new();

    /// <summary>Steps sent back for revision by the admin, if any.</summary>
    public string[]? RevisionSteps { get; set; }

    /// <summary>Admin note accompanying a revision request.</summary>
    public string? RevisionNote { get; set; }

    /// <summary>UTC timestamp when the provider submitted the onboarding for review.</summary>
    public string? SubmittedAtUtc { get; set; }

    /// <summary>Documents uploaded by the provider during the onboarding wizard. Enriched with signed URLs.</summary>
    public List<ProviderOnboardingDocumentBffDto> Documents { get; set; } = new();
}

[DocumentationInfo("Provider onboarding document BFF DTO", "Document uploaded by the provider during onboarding, with admin review status and signed URL.")]
public sealed class ProviderOnboardingDocumentBffDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public string? Issuer { get; set; }
    public string UploadedAt { get; set; } = string.Empty;

    /// <summary>Admin review decision for this document: Pending | Approved | Rejected</summary>
    public string? ReviewStatus { get; set; }

    /// <summary>Admin note when rejecting/requesting re-upload of a document.</summary>
    public string? ResolutionNote { get; set; }

    /// <summary>Short-lived signed GET URL (15 min TTL) from FileStorage. Null if FileStorage unavailable.</summary>
    public string? Url { get; set; }
}

// ── Revision request BFF DTO ──────────────────────────────────────────────────

[DocumentationInfo("Onboarding revision BFF response", "Response after admin requests provider to revise specific onboarding steps.")]
public sealed class OnboardingRevisionBffResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

// ── Shared approval ───────────────────────────────────────────────────────────

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
    public string? Url { get; set; }             // signed GET URL (15 min TTL); null if FileStorage unavailable
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
