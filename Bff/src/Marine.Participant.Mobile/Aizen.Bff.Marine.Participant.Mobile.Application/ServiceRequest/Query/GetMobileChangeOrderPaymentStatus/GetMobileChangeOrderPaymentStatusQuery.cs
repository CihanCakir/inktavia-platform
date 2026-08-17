using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/change-orders/{coId}/payment-status — the incremental escrow
/// transaction's payment status for an approved Increase (poll: Pending → Paid/Failed for the live-iyzico path). BFF
/// owner-gated. Cost-free. Reuses the MO3 payment-status contract pointed at the CO's own transaction.</summary>
public sealed class GetMobileChangeOrderPaymentStatusQuery : AizenQuery<MobilePaymentStatusDto>
{
    public GetMobileChangeOrderPaymentStatusQuery(long serviceRequestId, long changeOrderId)
    {
        ServiceRequestId = serviceRequestId;
        ChangeOrderId = changeOrderId;
    }

    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
}
