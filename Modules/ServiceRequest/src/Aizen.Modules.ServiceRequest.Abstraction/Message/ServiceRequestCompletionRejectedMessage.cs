using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request completion rejected message", "Published when the owner rejects the completion.")]
public sealed class ServiceRequestCompletionRejectedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long ProviderUserId { get; set; }
    public long OwnerUserId { get; set; }
    public string? ReviewNotes { get; set; }
}
