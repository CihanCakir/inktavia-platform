
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Complete service request response", "Result of completing a service request.")]
public sealed class CompleteServiceRequestResponse(long serviceRequestId)
{
    public long ServiceRequestId { get; } = serviceRequestId;
}
