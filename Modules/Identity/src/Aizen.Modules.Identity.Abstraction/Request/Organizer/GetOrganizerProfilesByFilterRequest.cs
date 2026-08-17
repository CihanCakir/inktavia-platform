namespace Aizen.Modules.Identity.Abstraction.Request.Organizer;

public class GetOrganizerProfilesByFilterRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? OnboardingStatus { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
