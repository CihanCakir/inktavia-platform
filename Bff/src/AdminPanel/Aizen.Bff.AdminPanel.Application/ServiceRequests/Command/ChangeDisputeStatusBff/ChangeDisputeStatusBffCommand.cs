using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class ChangeDisputeStatusBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ChangeServiceRequestDisputeStatusRequest Payload { get; }
    public ChangeDisputeStatusBffCommand(long serviceRequestId, long disputeId, ChangeServiceRequestDisputeStatusRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
    }
}
