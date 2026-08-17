using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Accept assignment command", "Provider accepts an assignment.")]
public sealed class AcceptServiceRequestAssignmentCommand : AizenCommand<AcceptServiceRequestAssignmentResponse>
{
    public long AssignmentId { get; }
    public AcceptServiceRequestAssignmentCommand(long assignmentId) => AssignmentId = assignmentId;
}
