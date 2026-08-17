using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Resolve dispute command", "Admin resolves a dispute.")]
public sealed class ResolveServiceRequestDisputeCommand : AizenCommand<ResolveServiceRequestDisputeResponse>
{
    public long DisputeId { get; }
    public ResolveServiceRequestDisputeRequest Request { get; }
    public ResolveServiceRequestDisputeCommand(long disputeId, ResolveServiceRequestDisputeRequest request)
    {
        DisputeId = disputeId; Request = request;
    }
}
