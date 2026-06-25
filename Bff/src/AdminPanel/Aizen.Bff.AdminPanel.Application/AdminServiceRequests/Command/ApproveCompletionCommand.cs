using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ApproveCompletionCommand : AizenCommand<ApproveServiceRequestCompletionResponse>
{
    public long ServiceRequestId { get; }
    public ApproveServiceRequestCompletionRequest Payload { get; }
    public string UserToken { get; }
    public ApproveCompletionCommand(long serviceRequestId, ApproveServiceRequestCompletionRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        UserToken = userToken;
    }
}
