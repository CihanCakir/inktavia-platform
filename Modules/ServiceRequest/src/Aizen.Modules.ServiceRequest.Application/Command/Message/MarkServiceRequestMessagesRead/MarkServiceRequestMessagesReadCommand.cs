using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;

namespace Aizen.Modules.ServiceRequest.Application.Command.Message;

[DocumentationInfo("Mark messages read command", "Marks a list of service request messages as read for the current user.")]
public sealed class MarkServiceRequestMessagesReadCommand : AizenCommand<bool>
{
    public long ServiceRequestId { get; }
    public MarkServiceRequestMessagesReadRequest Request { get; }

    public MarkServiceRequestMessagesReadCommand(long serviceRequestId, MarkServiceRequestMessagesReadRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }
}
