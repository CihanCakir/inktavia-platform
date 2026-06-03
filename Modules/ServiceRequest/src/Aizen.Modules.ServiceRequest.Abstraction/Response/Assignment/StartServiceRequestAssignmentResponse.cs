using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

[DocumentationInfo("Start assignment response", "Response after provider starts work on an assignment.")]
public sealed class StartServiceRequestAssignmentResponse(long assignmentId)
{
    public long AssignmentId { get; } = assignmentId;
    public bool Started { get; } = true;
}
