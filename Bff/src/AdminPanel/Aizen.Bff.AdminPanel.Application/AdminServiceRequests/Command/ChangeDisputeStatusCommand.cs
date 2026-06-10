using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ChangeDisputeStatusCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long ServiceRequestId { get; }
    public long DisputeId { get; }
    public ChangeServiceRequestDisputeStatusRequest Payload { get; }
    public string UserToken { get; }
    public ChangeDisputeStatusCommand(long serviceRequestId, long disputeId, ChangeServiceRequestDisputeStatusRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
        Payload = payload;
        UserToken = userToken;
    }
}
