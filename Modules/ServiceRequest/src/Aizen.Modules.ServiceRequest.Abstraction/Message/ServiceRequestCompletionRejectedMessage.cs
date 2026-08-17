using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request completion rejected message", "Published when the owner rejects the completion.")]
public sealed class ServiceRequestCompletionRejectedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long ProviderUserId { get; set; }
    public long OwnerUserId { get; set; }
    /// <summary>Free-text review notes (N-E).</summary>
    public string? ReviewNotes { get; set; }
    /// <summary>N-E structured reject reason (owner).</summary>
    public CompletionRejectReason? ReasonCode { get; set; }
}
