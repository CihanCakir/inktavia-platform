
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

[DocumentationInfo("Approve completion request", "Owner approves a provider's completion submission.")]
public sealed class ApproveServiceRequestCompletionRequest
{
    public string? ReviewNotes { get; set; }

    /// <summary>Optional owner satisfaction rating (1..5) captured at approval. When omitted the rating is left
    /// unchanged. Additive — never affects the SR→Completed transition or the decoupled escrow release.</summary>
    public int? ClientRating { get; set; }
}
