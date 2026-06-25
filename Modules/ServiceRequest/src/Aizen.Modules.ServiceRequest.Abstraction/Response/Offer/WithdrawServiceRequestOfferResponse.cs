using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Withdraw offer response", "Response after provider withdraws an offer.")]
public sealed class WithdrawServiceRequestOfferResponse(long offerId)
{
    public long OfferId { get; } = offerId;
    public bool Withdrawn { get; } = true;
}
