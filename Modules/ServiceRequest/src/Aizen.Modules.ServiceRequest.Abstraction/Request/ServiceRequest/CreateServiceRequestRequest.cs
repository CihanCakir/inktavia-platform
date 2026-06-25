using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Create service request request", "Input model for creating a new service request.")]
public sealed class CreateServiceRequestRequest
{
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ServiceRequestPriority Priority { get; set; } = ServiceRequestPriority.Normal;
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OwnerNotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<CreateServiceRequestItemRequest> Items { get; set; } = new();
}
