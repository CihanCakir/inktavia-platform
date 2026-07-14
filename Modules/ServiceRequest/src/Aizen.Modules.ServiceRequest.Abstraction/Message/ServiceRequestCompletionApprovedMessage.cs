using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request completion approved message", "Published when the owner approves the completion.")]
public sealed class ServiceRequestCompletionApprovedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long ProviderUserId { get; set; }
    public long OwnerUserId { get; set; }
}
