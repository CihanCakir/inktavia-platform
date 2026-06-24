using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class UpdateServiceRequestStatusCommand : AizenCommand<UpdateServiceRequestStatusResponse>
{
    public long ServiceRequestId { get; }
    public UpdateServiceRequestStatusRequest Payload { get; }
    public string UserToken { get; }

    public UpdateServiceRequestStatusCommand(long serviceRequestId, UpdateServiceRequestStatusRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        UserToken = userToken;
    }
}
