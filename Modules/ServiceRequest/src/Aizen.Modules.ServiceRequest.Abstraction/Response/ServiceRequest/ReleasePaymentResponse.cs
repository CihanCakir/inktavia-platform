using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Release payment response", "Result of releasing payment for a completed service request.")]
public sealed class ReleasePaymentResponse(long serviceRequestId)
{
    public long ServiceRequestId { get; } = serviceRequestId;
}
