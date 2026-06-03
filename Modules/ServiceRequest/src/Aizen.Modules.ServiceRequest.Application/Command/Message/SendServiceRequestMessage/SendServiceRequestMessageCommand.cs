using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

namespace Aizen.Modules.ServiceRequest.Application.Command.Message;

[DocumentationInfo("Send message command", "Sends a message scoped to a service request from a specific actor.")]
public sealed class SendServiceRequestMessageCommand : AizenCommand<SendServiceRequestMessageResponse>
{
    public long ServiceRequestId { get; }
    public ServiceRequestMessageSenderType SenderType { get; }
    public SendServiceRequestMessageRequest Request { get; }
    public SendServiceRequestMessageCommand(long serviceRequestId, ServiceRequestMessageSenderType senderType, SendServiceRequestMessageRequest request)
    {
        ServiceRequestId = serviceRequestId; SenderType = senderType; Request = request;
    }
}
