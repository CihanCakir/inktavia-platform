using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Change dispute status command", "Admin changes the status of an open dispute.")]
public sealed class ChangeServiceRequestDisputeStatusCommand : AizenCommand<bool>
{
    public long DisputeId { get; }
    public ChangeServiceRequestDisputeStatusRequest Request { get; }
    public ChangeServiceRequestDisputeStatusCommand(long disputeId, ChangeServiceRequestDisputeStatusRequest request)
    {
        DisputeId = disputeId; Request = request;
    }
}
