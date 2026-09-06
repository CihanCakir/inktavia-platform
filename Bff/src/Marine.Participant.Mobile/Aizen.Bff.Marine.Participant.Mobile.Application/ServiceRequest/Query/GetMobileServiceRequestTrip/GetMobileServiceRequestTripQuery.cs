using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/trip — the owner's live trip snapshot (null when no trip).</summary>
public sealed class GetMobileServiceRequestTripQuery : AizenQuery<MobileServiceRequestTripDto>
{
    public GetMobileServiceRequestTripQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
