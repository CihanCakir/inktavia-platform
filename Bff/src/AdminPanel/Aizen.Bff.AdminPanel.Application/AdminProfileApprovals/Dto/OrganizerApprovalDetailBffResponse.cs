using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Organizer approval detail BFF response", "Full organizer profile review data with applicant, company, checklist, documents and risk signals.")]
public sealed class OrganizerApprovalDetailBffResponse
{
    public OrganizerApprovalDetailBffDto? Organizer { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Organizer approval detail BFF DTO", "Organizer profile detail for the admin review screen.")]
public sealed class OrganizerApprovalDetailBffDto
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
    public string Status { get; set; } = null!;
    public string? ReviewedBy { get; set; }          // Gap: not in Identity DTO
    public string? ReviewedAt { get; set; }          // mapped from ApprovedAt or RejectedAt
    public string? RejectionCategory { get; set; }   // Gap: not in Identity DTO
    public string? RejectionReason { get; set; }     // mapped from RejectReason (detail-only endpoint)
    public string? InternalNote { get; set; }         // Gap: not in Identity DTO

    public OrganizerApplicantBffDto Applicant { get; set; } = new();
    public OrganizerCompanyBffDto Company { get; set; } = new();
    public ProfileApprovalChecklistBffDto Checklist { get; set; } = new();
    public List<ProfileApprovalDocumentBffDto> Documents { get; set; } = new();
    public List<ProfileApprovalRiskSignalBffDto> RiskSignals { get; set; } = new();
    public List<ProfileApprovalActivityItemBffDto> Activity { get; set; } = new();

    /// <summary>
    /// Provider onboarding wizard state fetched in parallel with profile data.
    /// Null when the provider has no onboarding record yet (e.g. not a Marine Provider) or when Identity is unavailable.
    /// </summary>
    public ProviderOnboardingBffDto? Onboarding { get; set; }

    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Organizer applicant BFF DTO", "Applicant personal info for the organizer approval review screen.")]
public sealed class OrganizerApplicantBffDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? RegisteredAt { get; set; }
    public string? IdentityType { get; set; }
    public string? Role { get; set; }
}

[DocumentationInfo("Organizer company BFF DTO", "Company/organization info for the organizer approval review screen.")]
public sealed class OrganizerCompanyBffDto
{
    // All fields are Gap: not available in Identity OrganizerProfile DTOs.
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? ContactPerson { get; set; }
    public string? BusinessCategory { get; set; }
    public string? BusinessLicenseNo { get; set; }
    public double? OperationalScore { get; set; }
    public string? EstimatedRevenue { get; set; }
    public string? HqLocation { get; set; }
}
