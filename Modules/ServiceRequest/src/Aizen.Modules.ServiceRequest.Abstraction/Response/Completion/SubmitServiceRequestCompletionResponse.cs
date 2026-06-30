using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

[DocumentationInfo("Submit completion response", "Response after provider submits completion evidence.")]
public sealed class SubmitServiceRequestCompletionResponse(ServiceRequestCompletionDto completion)
{
    public ServiceRequestCompletionDto Completion { get; } = completion;
}
