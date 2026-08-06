using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class CancelServiceRequestBffCommand : AizenCommand<CancelServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CancelServiceRequestRequest Payload { get; }
    public CancelServiceRequestBffCommand(long serviceRequestId, CancelServiceRequestRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
