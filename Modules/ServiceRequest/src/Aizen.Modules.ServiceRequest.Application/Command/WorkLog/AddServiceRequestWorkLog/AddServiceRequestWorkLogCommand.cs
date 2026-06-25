using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Add work log command", "Provider adds a work log entry to an assignment.")]
public sealed class AddServiceRequestWorkLogCommand : AizenCommand<AddServiceRequestWorkLogResponse>
{
    public long AssignmentId { get; }
    public AddServiceRequestWorkLogRequest Request { get; }
    public AddServiceRequestWorkLogCommand(long assignmentId, AddServiceRequestWorkLogRequest request)
    {
        AssignmentId = assignmentId; Request = request;
    }
}
