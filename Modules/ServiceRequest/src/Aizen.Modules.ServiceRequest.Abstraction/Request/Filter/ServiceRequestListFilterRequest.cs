using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

[DocumentationInfo("Service request list filter", "Pagination and filter parameters for owner service request list.")]
public sealed class ServiceRequestListFilterRequest
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public ServiceRequestStatus? Status { get; set; }
    public ServiceRequestPriority? Priority { get; set; }
    public long? VesselId { get; set; }
    public string? ServiceCategoryCode { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? SearchTerm { get; set; }
}
