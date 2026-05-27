namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public class UserProfileWithRolesDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? NationalityId { get; set; }
    public string RoleContext { get; set; } = null!;
    public string ApprovalStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreateDate { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
