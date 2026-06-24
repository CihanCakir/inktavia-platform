using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class AssignServiceRequestProviderCommand : AizenCommand<AssignProviderResponse>
{
    public long ServiceRequestId { get; }
    public AssignProviderRequest Payload { get; }
    public string UserToken { get; }

    public AssignServiceRequestProviderCommand(long serviceRequestId, AssignProviderRequest payload, string userToken)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
        UserToken = userToken;
    }
}
