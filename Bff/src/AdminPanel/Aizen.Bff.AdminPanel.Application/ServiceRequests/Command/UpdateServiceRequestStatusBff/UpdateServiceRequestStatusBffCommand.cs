using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class UpdateServiceRequestStatusBffCommand : AizenCommand<UpdateServiceRequestStatusResponse>
{
    public long ServiceRequestId { get; }
    public UpdateServiceRequestStatusRequest Payload { get; }

    public UpdateServiceRequestStatusBffCommand(long serviceRequestId, UpdateServiceRequestStatusRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
