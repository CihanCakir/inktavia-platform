using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/offers/{offerId}/reject — the owner rejects a received offer
/// with the N-E structured reason + optional note. Returns the re-read offer (status = Rejected). No money (MO3).</summary>
public sealed class RejectMobileServiceRequestOfferCommand : AizenCommand<MobileServiceRequestOfferDto>
{
    public RejectMobileServiceRequestOfferCommand(long serviceRequestId, long offerId, RejectMobileOfferRequest request)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public long OfferId { get; }
    public RejectMobileOfferRequest Request { get; }
}
