using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id} — full detail (request + timeline + attachments), gated to
/// the caller's own request. Cost-free (no offers/economics).</summary>
public sealed class GetMobileServiceRequestDetailQuery : AizenQuery<MobileServiceRequestDetailDto>
{
    public GetMobileServiceRequestDetailQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;

    public long ServiceRequestId { get; }
}
