namespace Aizen.Modules.Identity.Abstraction.Dto.Venue;

public class VenueProfileListItemDto
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
}
