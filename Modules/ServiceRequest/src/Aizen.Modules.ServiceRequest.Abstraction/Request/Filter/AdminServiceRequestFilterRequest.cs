using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

[DocumentationInfo("Admin service request filter", "Full filter parameters for admin-level service request listing.")]
public sealed class AdminServiceRequestFilterRequest
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public ServiceRequestStatus? Status { get; set; }
    public ServiceRequestPriority? Priority { get; set; }
    public long? OwnerUserId { get; set; }
    public long? ProviderProfileId { get; set; }
    public long? VesselId { get; set; }
    public string? ServiceCategoryCode { get; set; }
    public bool? HasDispute { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? SearchTerm { get; set; }
}
