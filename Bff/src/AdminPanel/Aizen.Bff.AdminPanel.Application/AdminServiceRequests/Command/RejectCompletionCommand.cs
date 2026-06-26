using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class RejectCompletionCommand : AizenCommand<RejectServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public RejectServiceRequestCompletionRequest Payload { get; }
    public RejectCompletionCommand(long serviceRequestId, RejectServiceRequestCompletionRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
