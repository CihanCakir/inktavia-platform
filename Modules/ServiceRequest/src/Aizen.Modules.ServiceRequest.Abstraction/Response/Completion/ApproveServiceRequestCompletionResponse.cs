using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

[DocumentationInfo("Approve completion response", "Response after owner approves a completion submission.")]
public sealed class ApproveServiceRequestCompletionResponse(long completionId)
{
    public long CompletionId { get; } = completionId;
    public bool Approved { get; } = true;
}
