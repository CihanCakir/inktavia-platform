using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>POST /api/v1/mobile/service-requests/{id}/offers/{offerId}/accept — the owner accepts a received offer.
/// The module runs BE-P8 economics + escrow (capture-at-accept) before committing; on Rejected/ConfigError it
/// throws (no half-accept). Returns the accepted offer's SR + the payment status read straight after accept.
/// Amounts/provider ids are server-owned — the body carries nothing money-related.</summary>
public sealed class AcceptMobileServiceRequestOfferCommand : AizenCommand<MobileAcceptOfferResultDto>
{
    public AcceptMobileServiceRequestOfferCommand(long serviceRequestId, long offerId)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
    }

    public long ServiceRequestId { get; }
    public long OfferId { get; }
}
