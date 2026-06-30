using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

[DocumentationInfo("Admin dispute filter", "Filter parameters for admin dispute listing.")]
public sealed class AdminDisputeFilterRequest
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public ServiceRequestDisputeStatus? Status { get; set; }
    public ServiceRequestDisputeReason? Reason { get; set; }
    public DateTime? OpenedFrom { get; set; }
    public DateTime? OpenedTo { get; set; }
}
