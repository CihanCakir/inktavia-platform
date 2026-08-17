namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;

[DocumentationInfo("Profile approval queue item BFF DTO", "Single row in the combined organizer/venue approval queue.")]
public sealed class ProfileApprovalQueueItemBffDto
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
    public string ProfileType { get; set; } = null!;         // "organizer" | "venue"
    public string? ApplicantName { get; set; }
    public string? CompanyOrVenueName { get; set; }          // Gap: not in Identity list DTO
    public string? Email { get; set; }                       // Gap: not in Identity list DTO
    public string? Phone { get; set; }                       // Gap: not in Identity list DTO
    public string? City { get; set; }                        // Gap: not in Identity list DTO
    public string? Country { get; set; }                     // Gap: not in Identity list DTO
    public string? SubmittedAt { get; set; }                 // ISO 8601 UTC — mapped from CreateDate
    public string? ReviewedAt { get; set; }                  // Gap: not in Identity list DTO
    public string Status { get; set; } = null!;              // "pending" | "approved" | "rejected" | "needsReview"
    public string StatusLabel { get; set; } = null!;
    public string? RiskLevel { get; set; }
    public double? DocumentCompletionPercent { get; set; }   // Gap: not available from Identity
    public string? OnboardingStatus { get; set; }
    public int DocumentCount { get; set; }
}
