using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>
/// BE-S11b — customer approves a change order → it is applied immediately (new snapshot + incremental escrow for an
/// Increase via P8/P9, or a P10 refund for a Decrease). Exercisable via API/bus without an owner app. Idempotent.
/// </summary>
[DocumentationInfo("Approve change order command", "Customer approves → applies the change order (incremental snapshot+escrow or P10 refund).")]
public sealed class ApproveServiceChangeOrderCommand : AizenCommand<ServiceChangeOrderDto>
{
    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
    public ApproveServiceChangeOrderCommand(long serviceRequestId, long changeOrderId)
    {
        ServiceRequestId = serviceRequestId; ChangeOrderId = changeOrderId;
    }
}
