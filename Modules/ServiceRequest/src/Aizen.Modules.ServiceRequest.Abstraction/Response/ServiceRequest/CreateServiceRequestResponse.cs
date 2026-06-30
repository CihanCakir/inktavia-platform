using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Create service request response", "Response after creating a new service request.")]
public sealed class CreateServiceRequestResponse(ServiceRequestDto serviceRequest)
{
    public ServiceRequestDto ServiceRequest { get; } = serviceRequest;
}
