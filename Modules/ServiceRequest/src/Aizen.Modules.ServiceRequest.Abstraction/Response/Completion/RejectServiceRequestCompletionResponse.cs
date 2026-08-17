
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

[DocumentationInfo("Reject completion response", "Response after owner rejects a completion submission.")]
public sealed class RejectServiceRequestCompletionResponse(long completionId)
{
    public long CompletionId { get; } = completionId;
    public bool Rejected { get; } = true;
}
