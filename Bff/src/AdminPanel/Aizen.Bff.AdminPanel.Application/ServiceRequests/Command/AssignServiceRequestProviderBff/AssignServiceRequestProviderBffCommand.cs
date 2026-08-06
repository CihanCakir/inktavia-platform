using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class AssignServiceRequestProviderBffCommand : AizenCommand<AssignProviderResponse>
{
    public long ServiceRequestId { get; }
    public AssignProviderRequest Payload { get; }

    public AssignServiceRequestProviderBffCommand(long serviceRequestId, AssignProviderRequest payload)
    {
        ServiceRequestId = serviceRequestId;
        Payload = payload;
    }
}
