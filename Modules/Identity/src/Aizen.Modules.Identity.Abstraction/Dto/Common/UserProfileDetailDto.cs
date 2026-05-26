namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public class UserProfileDetailDto
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
    public string RoleContext { get; set; } = null!;
    public string ApprovalStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
    public string TaxpayerType { get; set; } = null!;
    public DateTime? CreateDate { get; set; }
    public DateTime? ModifyDate { get; set; }
}
