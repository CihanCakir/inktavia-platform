
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Withdraw offer request", "Provider withdraws a submitted offer.")]
public sealed class WithdrawServiceRequestOfferRequest
{
    public string? Reason { get; set; }
}
