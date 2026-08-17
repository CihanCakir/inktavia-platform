using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>BE_MO2 — one received offer with its full cost-free breakdown (for the owner offer-detail screen).</summary>
public sealed class GetMobileServiceRequestOfferQuery : AizenQuery<MobileServiceRequestOfferDto>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }

    public GetMobileServiceRequestOfferQuery(long serviceRequestId, long offerId)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
    }
}
