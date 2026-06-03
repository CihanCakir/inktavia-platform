using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Reject completion command", "Owner rejects a provider's completion submission.")]
public sealed class RejectServiceRequestCompletionCommand : AizenCommand<RejectServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public RejectServiceRequestCompletionRequest Request { get; }
    public RejectServiceRequestCompletionCommand(long serviceRequestId, RejectServiceRequestCompletionRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
