using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/change-orders/{coId}/reject — the owner rejects a proposed change
/// order (optional free-text reason). Reuses the S11 reject verbatim — terminal, no economics. BFF owner-gated.
/// Returns the rejected CO.</summary>
public sealed class RejectMobileChangeOrderCommand : AizenCommand<MobileChangeOrderDto>
{
    public RejectMobileChangeOrderCommand(long serviceRequestId, long changeOrderId, MobileRejectChangeOrderRequest request)
    {
        ServiceRequestId = serviceRequestId;
        ChangeOrderId = changeOrderId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
    public MobileRejectChangeOrderRequest Request { get; }
}
