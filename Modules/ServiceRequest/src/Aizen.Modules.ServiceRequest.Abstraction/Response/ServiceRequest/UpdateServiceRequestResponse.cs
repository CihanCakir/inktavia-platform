using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Update service request response", "Response after updating a service request.")]
public sealed class UpdateServiceRequestResponse(ServiceRequestDto serviceRequest)
{
    public ServiceRequestDto ServiceRequest { get; } = serviceRequest;
}
