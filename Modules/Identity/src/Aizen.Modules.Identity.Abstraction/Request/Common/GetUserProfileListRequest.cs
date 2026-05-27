namespace Aizen.Modules.Identity.Abstraction.Request.Common;

public class GetUserProfileListRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RoleContext { get; set; }
    public string? ApprovalStatus { get; set; }
}
