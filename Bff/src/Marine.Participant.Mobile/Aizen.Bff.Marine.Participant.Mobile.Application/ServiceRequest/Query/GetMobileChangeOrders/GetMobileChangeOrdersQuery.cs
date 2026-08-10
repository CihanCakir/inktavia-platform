using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/{id}/change-orders — the owner's change orders on their own accepted
/// SR (direction, changed lines, incremental ₺, status) + the derived effective total. BFF owner-gated. Cost-free.</summary>
public sealed class GetMobileChangeOrdersQuery : AizenQuery<MobileChangeOrderListDto>
{
    public GetMobileChangeOrdersQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;

    public long ServiceRequestId { get; }
}
