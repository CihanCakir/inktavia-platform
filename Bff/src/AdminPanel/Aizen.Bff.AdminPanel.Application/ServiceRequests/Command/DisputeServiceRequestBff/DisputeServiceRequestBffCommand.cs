using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class DisputeServiceRequestBffCommand : AizenCommand<DisputeServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public DisputeServiceRequestRequest Payload { get; }

    public DisputeServiceRequestBffCommand(long serviceRequestId, DisputeServiceRequestRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
