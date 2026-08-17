
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Accept offer request", "Owner accepts a provider offer.")]
public sealed class AcceptServiceRequestOfferRequest
{
    public long OfferId { get; set; }
}
