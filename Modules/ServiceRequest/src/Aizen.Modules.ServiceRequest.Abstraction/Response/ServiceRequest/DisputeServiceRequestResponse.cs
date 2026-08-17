
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Dispute service request response", "Result of opening a dispute on a service request.")]
public sealed class DisputeServiceRequestResponse(long serviceRequestId)
{
    public long ServiceRequestId { get; } = serviceRequestId;
}
