using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ResolveDisputeCommand : AizenCommand<ResolveServiceRequestDisputeResponse>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ResolveServiceRequestDisputeRequest Payload { get; }
    public ResolveDisputeCommand(long serviceRequestId, long disputeId, ResolveServiceRequestDisputeRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
    }
}
