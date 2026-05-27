namespace Aizen.Modules.Identity.Abstraction.Request.Venue;

public class GetVenueProfilesByFilterRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApprovalStatus { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
