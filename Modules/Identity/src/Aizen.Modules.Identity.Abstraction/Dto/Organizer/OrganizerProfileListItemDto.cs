namespace Aizen.Modules.Identity.Abstraction.Dto.Organizer;

public class OrganizerProfileListItemDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
    public string TaxpayerType { get; set; } = null!;
    public string ApprovalStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreateDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? OrganizationName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ReviewedAt { get; set; }
    public string RiskLevel { get; set; } = "L";
    public DateTime? SubmittedAtUtc { get; set; }
    public string? OnboardingStatus { get; set; }
    public int DocumentCount { get; set; }
}
