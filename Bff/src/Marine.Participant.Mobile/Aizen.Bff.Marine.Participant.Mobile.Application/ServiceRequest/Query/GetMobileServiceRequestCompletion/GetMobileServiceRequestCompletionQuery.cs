using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/completion — the owner reads the provider's completion (notes +
/// evidence + the auto-approve countdown source) for one of their own SRs. BFF owner-gated. Cost-free. Returns null
/// when no completion exists yet (the FE hides the review section).</summary>
public sealed class GetMobileServiceRequestCompletionQuery : AizenQuery<MobileServiceRequestCompletionDto>
{
    public GetMobileServiceRequestCompletionQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;

    public long ServiceRequestId { get; }
}
