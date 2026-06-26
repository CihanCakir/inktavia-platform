using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class DisputeServiceRequestAdminCommand : AizenCommand<DisputeServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public DisputeServiceRequestRequest Payload { get; }

    public DisputeServiceRequestAdminCommand(long serviceRequestId, DisputeServiceRequestRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
