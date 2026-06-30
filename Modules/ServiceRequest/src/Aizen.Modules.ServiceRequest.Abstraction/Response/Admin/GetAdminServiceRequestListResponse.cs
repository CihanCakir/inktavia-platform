using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

[DocumentationInfo("Admin service request list response", "Response for admin-level service request listing with full filter support.")]
public sealed class GetAdminServiceRequestListResponse(List<ServiceRequestSummaryDto> items, int totalCount)
{
    public List<ServiceRequestSummaryDto> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
}
