using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.Identity.Abstraction.Dto.Organizer;

public class OrganizerProfileDetailDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? NationalityId { get; set; }
    public string TaxpayerType { get; set; } = null!;
    public string ApprovalStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? ModifyDate { get; set; }
    public string? OrganizationName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewedAt { get; set; }
    public string? RejectionCategory { get; set; }
    public string RiskLevel { get; set; } = "L";
    public List<VerificationDocumentDto> Documents { get; set; } = new();
    public List<RiskSignalDto> RiskSignals { get; set; } = new();
}
