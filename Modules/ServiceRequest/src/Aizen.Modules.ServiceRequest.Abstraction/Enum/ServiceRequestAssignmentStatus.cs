using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest assignment status enum", "Lifecycle status values for a service request assignment.")]
public enum ServiceRequestAssignmentStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Scheduled = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7
}
