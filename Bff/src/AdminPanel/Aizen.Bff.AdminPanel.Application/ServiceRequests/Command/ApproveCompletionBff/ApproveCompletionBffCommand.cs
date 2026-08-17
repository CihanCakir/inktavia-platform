using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class ApproveCompletionBffCommand : AizenCommand<ApproveServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public ApproveServiceRequestCompletionRequest Payload { get; }
    public ApproveCompletionBffCommand(long serviceRequestId, ApproveServiceRequestCompletionRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
