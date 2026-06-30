using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Get service request list response", "Paginated response for service request list queries.")]
public sealed class GetServiceRequestListResponse(List<ServiceRequestSummaryDto> items, int totalCount)
{
    public List<ServiceRequestSummaryDto> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
}
