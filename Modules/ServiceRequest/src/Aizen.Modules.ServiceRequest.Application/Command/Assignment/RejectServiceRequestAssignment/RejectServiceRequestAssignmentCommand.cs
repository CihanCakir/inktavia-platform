using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Reject assignment command", "Provider rejects an assignment.")]
public sealed class RejectServiceRequestAssignmentCommand : AizenCommand<RejectServiceRequestAssignmentResponse>
{
    public long AssignmentId { get; }
    public RejectServiceRequestAssignmentRequest Request { get; }
    public RejectServiceRequestAssignmentCommand(long assignmentId, RejectServiceRequestAssignmentRequest request)
    {
        AssignmentId = assignmentId; Request = request;
    }
}
