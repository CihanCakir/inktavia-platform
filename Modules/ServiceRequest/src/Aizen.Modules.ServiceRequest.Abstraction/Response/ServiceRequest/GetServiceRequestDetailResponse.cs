using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Get service request detail response", "Response containing full service request detail.")]
public sealed class GetServiceRequestDetailResponse(ServiceRequestDetailDto detail)
{
    public ServiceRequestDetailDto Detail { get; } = detail;
}
