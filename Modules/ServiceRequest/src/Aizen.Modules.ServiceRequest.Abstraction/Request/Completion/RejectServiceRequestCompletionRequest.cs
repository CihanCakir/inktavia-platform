
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

[DocumentationInfo("Reject completion request", "Owner rejects a provider's completion submission.")]
public sealed class RejectServiceRequestCompletionRequest
{
    public string? ReviewNotes { get; set; }
}
