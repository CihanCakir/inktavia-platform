
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Assign provider response", "Result of assigning a provider to a service request.")]
public sealed class AssignProviderResponse(long serviceRequestId, long providerId)
{
    public long ServiceRequestId { get; } = serviceRequestId;
    public long ProviderId { get; } = providerId;
}
