namespace Aizen.Modules.Identity.Abstraction.Request.Organizer;

public class GetOrganizerProfilesByFilterRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApprovalStatus { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
