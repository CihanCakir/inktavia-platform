using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/dispute/{disputeId}/case — the owner reads the cost-free dispute
/// case (read-only). BFF owner-gated (the SR must belong to the caller); the underlying module case query is ungated
/// by design. Cost-free — no supplier cost / dealer margin / commission / provider-net / user ids. Resolution
/// outcome surfaces only once the admin resolves (the owner never resolves).</summary>
public sealed class GetMobileDisputeCaseQuery : AizenQuery<MobileDisputeCaseDto>
{
    public GetMobileDisputeCaseQuery(long serviceRequestId, long disputeId)
    {
        ServiceRequestId = serviceRequestId;
        DisputeId = disputeId;
    }

    public long ServiceRequestId { get; }
    public long DisputeId { get; }
}
