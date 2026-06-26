using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class CancelServiceRequestCommand : AizenCommand<CancelServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CancelServiceRequestRequest Payload { get; }
    public CancelServiceRequestCommand(long serviceRequestId, CancelServiceRequestRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
