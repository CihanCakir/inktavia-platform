using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

[DocumentationInfo("Reject completion request", "Owner rejects a provider's completion submission.")]
public sealed class RejectServiceRequestCompletionRequest
{
    /// <summary>N-E structured reject reason (owner).</summary>
    public CompletionRejectReason? ReasonCode { get; set; }

    /// <summary>Optional free-text review notes (kept alongside the structured reason).</summary>
    public string? ReviewNotes { get; set; }
}
