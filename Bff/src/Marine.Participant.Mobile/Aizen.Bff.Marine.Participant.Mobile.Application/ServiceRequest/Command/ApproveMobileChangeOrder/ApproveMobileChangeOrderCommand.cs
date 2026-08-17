using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/change-orders/{coId}/approve — the owner approves a change order.
/// Reuses the S11 apply engine verbatim: Increase → new incremental snapshot + capture-at-approve; Decrease → P10
/// refund of the delta; a P5/S9 breach flips the CO to Rejected (no capture). BFF owner-gated; amounts server-owned.
/// Returns the applied CO + the incremental payment status (already Paid on the manual gateway; pollable for live
/// iyzico).</summary>
public sealed class ApproveMobileChangeOrderCommand : AizenCommand<MobileChangeOrderApproveResultDto>
{
    public ApproveMobileChangeOrderCommand(long serviceRequestId, long changeOrderId)
    {
        ServiceRequestId = serviceRequestId;
        ChangeOrderId = changeOrderId;
    }

    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
}
