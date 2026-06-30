using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Approve completion command", "Owner approves a provider's completion submission.")]
public sealed class ApproveServiceRequestCompletionCommand : AizenCommand<ApproveServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public ApproveServiceRequestCompletionRequest Request { get; }
    public ApproveServiceRequestCompletionCommand(long serviceRequestId, ApproveServiceRequestCompletionRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
