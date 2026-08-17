using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

[DocumentationInfo("Open dispute response", "Response after a dispute is opened on a service request.")]
public sealed class OpenServiceRequestDisputeResponse(ServiceRequestDisputeDto dispute)
{
    public ServiceRequestDisputeDto Dispute { get; } = dispute;
}
