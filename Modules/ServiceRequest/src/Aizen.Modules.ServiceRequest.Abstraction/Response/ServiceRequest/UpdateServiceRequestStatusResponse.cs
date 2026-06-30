
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Update service request status response", "Result of a status change operation.")]
public sealed class UpdateServiceRequestStatusResponse(long serviceRequestId, string newStatus)
{
    public long ServiceRequestId { get; } = serviceRequestId;
    public string NewStatus { get; } = newStatus;
}
