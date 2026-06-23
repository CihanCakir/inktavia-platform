namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public class UserProfileListItemDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
    public string RoleContext { get; set; } = null!;
    public string ApprovalStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreateDate { get; set; }
}
