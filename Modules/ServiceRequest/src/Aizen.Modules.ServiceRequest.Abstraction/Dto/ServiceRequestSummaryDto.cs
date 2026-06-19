using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

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
