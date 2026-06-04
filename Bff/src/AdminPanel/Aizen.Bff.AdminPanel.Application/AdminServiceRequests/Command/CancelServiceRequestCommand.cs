using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class CancelServiceRequestCommand : AizenCommand<CancelServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CancelServiceRequestRequest Payload { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public CancelServiceRequestCommand(long serviceRequestId, CancelServiceRequestRequest payload, string authorization, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        Authorization = authorization;
        UserToken = userToken;
    }
}
