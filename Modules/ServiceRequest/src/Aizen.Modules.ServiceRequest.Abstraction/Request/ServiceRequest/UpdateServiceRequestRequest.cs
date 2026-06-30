using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Update service request request", "Input model for updating an existing service request.")]
public sealed class UpdateServiceRequestRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public ServiceRequestPriority Priority { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OwnerNotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
