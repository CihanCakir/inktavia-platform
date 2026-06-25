using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest completion status enum", "Review status of a completion submission for a service request.")]
public enum ServiceRequestCompletionStatus
{
    Submitted = 1,
    ApprovedByOwner = 2,
    RejectedByOwner = 3,
    DisputedByOwner = 4,
    ClosedByAdmin = 5
}
