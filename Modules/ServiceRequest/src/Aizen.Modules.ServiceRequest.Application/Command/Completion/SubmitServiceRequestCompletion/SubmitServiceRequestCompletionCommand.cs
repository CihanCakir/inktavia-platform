using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Submit completion command", "Provider submits completion evidence for owner review.")]
public sealed class SubmitServiceRequestCompletionCommand : AizenCommand<SubmitServiceRequestCompletionResponse>
{
    public long AssignmentId { get; }
    public SubmitServiceRequestCompletionRequest Request { get; }
    public SubmitServiceRequestCompletionCommand(long assignmentId, SubmitServiceRequestCompletionRequest request)
    {
        AssignmentId = assignmentId; Request = request;
    }
}
