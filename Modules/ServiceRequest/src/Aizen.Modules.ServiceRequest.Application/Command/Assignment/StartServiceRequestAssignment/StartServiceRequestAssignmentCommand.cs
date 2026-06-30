using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Start assignment command", "Provider starts work on an accepted assignment.")]
public sealed class StartServiceRequestAssignmentCommand : AizenCommand<StartServiceRequestAssignmentResponse>
{
    public long AssignmentId { get; }
    public StartServiceRequestAssignmentCommand(long assignmentId) => AssignmentId = assignmentId;
}
