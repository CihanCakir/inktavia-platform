using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Command;

public sealed class ReleasePaymentAdminCommand : AizenCommand<ReleasePaymentResponse>
{
    public long ServiceRequestId { get; }

    public ReleasePaymentAdminCommand(long serviceRequestId)
    {
        ServiceRequestId = serviceRequestId;
    }
}
