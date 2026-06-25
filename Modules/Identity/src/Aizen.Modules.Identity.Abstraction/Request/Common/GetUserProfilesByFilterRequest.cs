namespace Aizen.Modules.Identity.Abstraction.Request.Common;

public class GetUserProfilesByFilterRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RoleContext { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? Status { get; set; }
    public string? Email { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
