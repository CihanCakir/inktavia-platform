using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Venue approval detail BFF response", "Full venue profile review data with owner, venue info, location, checklist, documents and risk signals.")]
public sealed class VenueApprovalDetailBffResponse
{
    public VenueApprovalDetailBffDto? Venue { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Venue approval detail BFF DTO", "Venue profile detail for the admin review screen.")]
public sealed class VenueApprovalDetailBffDto
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
    public string Status { get; set; } = null!;
    public string? ReviewedBy { get; set; }          // Gap: not in Identity DTO
    public string? ReviewedAt { get; set; }          // mapped from ApprovedAt or RejectedAt
    public string? RejectionCategory { get; set; }   // Gap: not in Identity DTO
    public string? RejectionReason { get; set; }     // mapped from RejectReason (detail-only endpoint)
    public string? InternalNote { get; set; }         // Gap: not in Identity DTO

    public VenueOwnerBffDto Owner { get; set; } = new();
    public VenueDetailBffDto Venue { get; set; } = new();
    public VenueLocationBffDto Location { get; set; } = new();
    public ProfileApprovalChecklistBffDto Checklist { get; set; } = new();
    public List<ProfileApprovalDocumentBffDto> Documents { get; set; } = new();
    public List<ProfileApprovalRiskSignalBffDto> RiskSignals { get; set; } = new();
    public List<ProfileApprovalActivityItemBffDto> Activity { get; set; } = new();
}

[DocumentationInfo("Venue owner BFF DTO", "Owner personal info for the venue approval review screen.")]
public sealed class VenueOwnerBffDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? RegisteredAt { get; set; }
}

[DocumentationInfo("Venue detail BFF DTO", "Venue-specific info for the venue approval review screen.")]
public sealed class VenueDetailBffDto
{
    // All fields are Gap: not available in Identity VenueProfile DTOs.
    public string? VenueName { get; set; }
    public string? VenueType { get; set; }
    public int? Capacity { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ContactPerson { get; set; }
    public string? BusinessRegistrationNo { get; set; }
    public string? TaxNo { get; set; }
    public string? Website { get; set; }
    public string? OperationalHours { get; set; }
    public int? SecurityTier { get; set; }
    public string? MemberId { get; set; }
    public double? Rating { get; set; }
    public int? EventCount { get; set; }
    public string? Revenue { get; set; }
}

[DocumentationInfo("Venue location BFF DTO", "Location coordinates and address info for the venue approval review screen.")]
public sealed class VenueLocationBffDto
{
    public double? Latitude { get; set; }      // Gap: not in Identity DTO
    public double? Longitude { get; set; }     // Gap: not in Identity DTO
    public bool AddressVerified { get; set; }
    public string? DisplayText { get; set; }
}
