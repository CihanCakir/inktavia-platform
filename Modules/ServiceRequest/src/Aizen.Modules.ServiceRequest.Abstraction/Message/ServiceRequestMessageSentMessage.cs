using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request message sent message", "Published when a participant sends a message.")]
public sealed class ServiceRequestMessageSentMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long MessageId { get; set; }
    public long SenderUserId { get; set; }
    public ServiceRequestMessageSenderType SenderType { get; set; }
    public long? ProviderProfileId { get; set; }
}
