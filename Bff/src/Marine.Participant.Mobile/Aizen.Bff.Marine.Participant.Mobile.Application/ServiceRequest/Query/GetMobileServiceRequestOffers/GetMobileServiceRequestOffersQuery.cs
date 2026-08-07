using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>BE_MO2 — the offers received on one of the caller's own service requests (cost-free).</summary>
public sealed class GetMobileServiceRequestOffersQuery : AizenQuery<List<MobileServiceRequestOfferDto>>
{
    public long ServiceRequestId { get; }
    public GetMobileServiceRequestOffersQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
