using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class ResolveDisputeBffCommand : AizenCommand<ResolveServiceRequestDisputeResponse>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ResolveServiceRequestDisputeRequest Payload { get; }
    public ResolveDisputeBffCommand(long serviceRequestId, long disputeId, ResolveServiceRequestDisputeRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
    }
}
