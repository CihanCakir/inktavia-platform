using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

[DocumentationInfo("Reject assignment response", "Response after provider rejects an assignment.")]
public sealed class RejectServiceRequestAssignmentResponse(long assignmentId)
{
    public long AssignmentId { get; } = assignmentId;
    public bool Rejected { get; } = true;
}
