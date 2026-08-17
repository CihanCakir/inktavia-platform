using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class CompleteServiceRequestAdminCommand : AizenCommand<CompleteServiceRequestResponse>
{
    public long ServiceRequestId { get; }

    public CompleteServiceRequestAdminCommand(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
