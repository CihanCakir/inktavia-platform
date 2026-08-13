using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/payment-status — the owner polls the payment lifecycle of
/// their accepted SR (Pending → Paid/Failed). BFF owner-gated. Cost-free (customer total + status only).</summary>
public sealed class GetMobileServiceRequestPaymentStatusQuery : AizenQuery<MobilePaymentStatusDto>
{
    public GetMobileServiceRequestPaymentStatusQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;

    public long ServiceRequestId { get; }
}
