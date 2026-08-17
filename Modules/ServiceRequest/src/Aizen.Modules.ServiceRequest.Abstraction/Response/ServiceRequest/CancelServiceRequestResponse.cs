
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Cancel service request response", "Response after cancelling a service request.")]
public sealed class CancelServiceRequestResponse(long serviceRequestId)
{
    public long ServiceRequestId { get; } = serviceRequestId;
    public bool Cancelled { get; } = true;
}
