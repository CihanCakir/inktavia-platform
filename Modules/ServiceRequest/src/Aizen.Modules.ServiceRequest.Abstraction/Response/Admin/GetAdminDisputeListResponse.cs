using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

[DocumentationInfo("Admin dispute list response", "Response for admin dispute listing.")]
public sealed class GetAdminDisputeListResponse(List<ServiceRequestDisputeDto> disputes, int totalCount)
{
    public List<ServiceRequestDisputeDto> Disputes { get; } = disputes;
    public int TotalCount { get; } = totalCount;
}
