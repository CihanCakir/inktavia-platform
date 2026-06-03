using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Reject offer response", "Response after owner rejects an offer.")]
public sealed class RejectServiceRequestOfferResponse(long offerId)
{
    public long OfferId { get; } = offerId;
    public bool Rejected { get; } = true;
}
