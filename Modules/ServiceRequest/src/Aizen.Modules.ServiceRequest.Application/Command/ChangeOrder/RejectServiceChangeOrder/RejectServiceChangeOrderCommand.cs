using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>BE-S11b — customer rejects a proposed change order (terminal; no economics). Exercisable via API/bus.</summary>
[DocumentationInfo("Reject change order command", "Customer rejects a proposed change order (terminal, no economics).")]
public sealed class RejectServiceChangeOrderCommand : AizenCommand<ServiceChangeOrderDto>
{
    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
    public RejectServiceChangeOrderRequest Request { get; }
    public RejectServiceChangeOrderCommand(long serviceRequestId, long changeOrderId, RejectServiceChangeOrderRequest request)
    {
        ServiceRequestId = serviceRequestId; ChangeOrderId = changeOrderId; Request = request;
    }
}
