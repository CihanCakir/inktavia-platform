using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request completion submitted message", "Published when provider submits completion.")]
public sealed class ServiceRequestCompletionSubmittedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderUserId { get; set; }
}
