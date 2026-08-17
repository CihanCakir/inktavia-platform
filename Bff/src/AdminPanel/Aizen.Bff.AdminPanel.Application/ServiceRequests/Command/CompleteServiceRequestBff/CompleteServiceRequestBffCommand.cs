using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class CompleteServiceRequestBffCommand : AizenCommand<CompleteServiceRequestResponse>
{
    public long ServiceRequestId { get; }

    public CompleteServiceRequestBffCommand(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
