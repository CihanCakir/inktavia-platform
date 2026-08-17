
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest dispute status enum", "Lifecycle status values for a dispute opened on a service request.")]
public enum ServiceRequestDisputeStatus
{
    Open = 1,
    UnderReview = 2,
    PendingOwnerResponse = 3,
    PendingProviderResponse = 4,
    Escalated = 5,
    Resolved = 6,
    Closed = 7
}
