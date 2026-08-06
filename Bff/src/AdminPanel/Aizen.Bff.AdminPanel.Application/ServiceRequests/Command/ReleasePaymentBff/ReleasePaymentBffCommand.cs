using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

public sealed class ReleasePaymentBffCommand : AizenCommand<ReleasePaymentResponse>
{
    public long ServiceRequestId { get; }

    public ReleasePaymentBffCommand(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
