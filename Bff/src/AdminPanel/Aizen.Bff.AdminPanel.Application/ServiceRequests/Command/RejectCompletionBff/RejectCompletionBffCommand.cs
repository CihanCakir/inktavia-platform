using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class RejectCompletionBffCommand : AizenCommand<RejectServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public RejectServiceRequestCompletionRequest Payload { get; }
    public RejectCompletionBffCommand(long serviceRequestId, RejectServiceRequestCompletionRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
