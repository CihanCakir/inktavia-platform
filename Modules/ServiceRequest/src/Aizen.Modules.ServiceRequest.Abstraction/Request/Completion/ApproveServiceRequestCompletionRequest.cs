using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

[DocumentationInfo("Approve completion request", "Owner approves a provider's completion submission.")]
public sealed class ApproveServiceRequestCompletionRequest
{
    public string? ReviewNotes { get; set; }
}
