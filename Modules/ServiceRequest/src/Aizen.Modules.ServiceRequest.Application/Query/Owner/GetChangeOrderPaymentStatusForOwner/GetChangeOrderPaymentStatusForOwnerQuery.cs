using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetChangeOrderPaymentStatusForOwner;

/// <summary>
/// BE-MO6 — the owner-facing payment status of a change order's INCREMENTAL escrow transaction (Increase). The
/// incremental capture-at-approve stores its transaction on the change order (<c>co.PaymentTransactionId</c>), NOT on
/// the SR — so the MO3 SR payment-status poll (which reads the original acceptance escrow) can't report it. This
/// query reuses the SAME MO3 transaction-status seam, but pointed at the change order's own transaction. Owner-scoped;
/// returns the shared owner payment-status response (<c>None</c> when the CO has no transaction yet).
/// </summary>
[DocumentationInfo("Get change-order payment status query", "Owner-facing payment lifecycle of a change order's incremental escrow transaction.")]
public sealed class GetChangeOrderPaymentStatusForOwnerQuery : AizenQuery<GetServiceRequestPaymentStatusForOwnerResponse>
{
    public GetChangeOrderPaymentStatusForOwnerQuery(long serviceRequestId, long changeOrderId)
    {
        ServiceRequestId = serviceRequestId;
        ChangeOrderId = changeOrderId;
    }

    public long ServiceRequestId { get; }
    public long ChangeOrderId { get; }
}
