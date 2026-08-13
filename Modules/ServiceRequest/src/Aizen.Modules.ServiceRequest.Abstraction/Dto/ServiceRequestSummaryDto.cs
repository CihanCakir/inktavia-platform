using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest summary DTO", "Lean list view DTO for service request listings.")]
public sealed class ServiceRequestSummaryDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public ServiceRequestStatus Status { get; set; }
    public ServiceRequestPriority Priority { get; set; }
    public long VesselId { get; set; }
    public long OwnerUserId { get; set; }
    public long? ProviderProfileId { get; set; }
    /// <summary>
    /// Assigned provider's USER id (from the active assignment; null when unassigned). Additive data-completeness
    /// field so the BFF can resolve the provider display name via Identity (which keys by user id, not profile id).
    /// </summary>
    public long? ProviderUserId { get; set; }
    public string? LocationMarinaName { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? OwnerNotes { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int OfferCount { get; set; }
    public bool HasActiveAssignment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
