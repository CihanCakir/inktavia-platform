using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestPaymentStatusForOwner;

/// <summary>
/// BE-MO3 — the owner-facing payment status of an accepted SR (poll after accept: Pending → Paid/Failed).
/// Owner-scoped: the handler verifies the caller owns the SR (OwnerUserId from the trusted context), never from
/// parameters. Reuses the Payment transaction-status read; cost-free.
/// </summary>
[DocumentationInfo("Get payment status for owner query", "Payment lifecycle state of the caller-owner's accepted service request.")]
public sealed class GetServiceRequestPaymentStatusForOwnerQuery : AizenQuery<GetServiceRequestPaymentStatusForOwnerResponse>
{
    public long ServiceRequestId { get; }

    public GetServiceRequestPaymentStatusForOwnerQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
