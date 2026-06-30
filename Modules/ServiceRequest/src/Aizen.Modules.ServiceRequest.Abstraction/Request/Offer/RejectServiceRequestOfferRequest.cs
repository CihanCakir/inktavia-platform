
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Reject offer request", "Owner rejects a provider offer.")]
public sealed class RejectServiceRequestOfferRequest
{
    public long OfferId { get; set; }
    public string? Reason { get; set; }
}
