
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

[DocumentationInfo("Accept assignment response", "Response after provider accepts an assignment.")]
public sealed class AcceptServiceRequestAssignmentResponse(long assignmentId)
{
    public long AssignmentId { get; } = assignmentId;
    public bool Accepted { get; } = true;
}
