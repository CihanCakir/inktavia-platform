
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

[DocumentationInfo("Submit completion request", "Provider submits completion evidence for owner review.")]
public sealed class SubmitServiceRequestCompletionRequest
{
    public string? CompletionNotes { get; set; }
    public Guid? EvidenceFileId { get; set; }
}
