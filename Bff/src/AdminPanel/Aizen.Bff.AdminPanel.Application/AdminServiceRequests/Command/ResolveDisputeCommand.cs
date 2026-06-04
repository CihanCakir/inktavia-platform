using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ResolveDisputeCommand : AizenCommand<ResolveServiceRequestDisputeResponse>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ResolveServiceRequestDisputeRequest Payload { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public ResolveDisputeCommand(long serviceRequestId, long disputeId, ResolveServiceRequestDisputeRequest payload, string authorization, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
        Authorization = authorization;
        UserToken = userToken;
    }
}
