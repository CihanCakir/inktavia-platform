namespace Aizen.Bff.MarineProvider.Application.Contracts.Me;

public sealed class ProviderProfileDto
{
    public long ProviderProfileId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? TaxpayerType { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public string ProfileStatus { get; set; } = default!;
    public int DocumentCount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
}
