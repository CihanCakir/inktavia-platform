
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Accept offer response", "Response after owner accepts an offer.")]
public sealed class AcceptServiceRequestOfferResponse(long offerId, long serviceRequestId)
{
    public long OfferId { get; } = offerId;
    public long ServiceRequestId { get; } = serviceRequestId;
    public bool Accepted { get; } = true;
}
