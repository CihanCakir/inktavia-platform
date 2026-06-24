using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Add work log entry command", "Adds a simple work log entry to a service request without requiring an assignment.")]
public sealed class AddWorkLogEntryCommand : AizenCommand<AddWorkLogEntryResponse>
{
    public long ServiceRequestId { get; }
    public AddWorkLogEntryRequest Request { get; }
    public AddWorkLogEntryCommand(long serviceRequestId, AddWorkLogEntryRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
