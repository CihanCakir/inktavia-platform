using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

[DocumentationInfo("Send message response", "Response after sending a service request message.")]
public sealed class SendServiceRequestMessageResponse(ServiceRequestMessageDto message)
{
    public ServiceRequestMessageDto Message { get; } = message;
}
