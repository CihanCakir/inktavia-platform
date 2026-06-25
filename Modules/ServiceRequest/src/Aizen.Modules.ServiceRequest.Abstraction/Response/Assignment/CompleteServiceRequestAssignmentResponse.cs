using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

[DocumentationInfo("Complete assignment response", "Response after provider completes an assignment.")]
public sealed class CompleteServiceRequestAssignmentResponse(long assignmentId)
{
    public long AssignmentId { get; } = assignmentId;
    public bool Completed { get; } = true;
}
