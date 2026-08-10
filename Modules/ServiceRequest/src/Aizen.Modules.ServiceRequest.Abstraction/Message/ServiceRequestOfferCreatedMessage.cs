using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request offer created message", "Published when a provider creates an offer.")]
public sealed class ServiceRequestOfferCreatedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long ProviderProfileId { get; set; }
    public long ProviderUserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// BE_WC1b — the offer's lifecycle status at publish time. The real create path (<c>CreateServiceRequestOffer</c>)
    /// calls <c>offer.Submit()</c> so this is <c>Submitted</c>; a draft create would be <c>Draft</c>. The Messaging
    /// OFFER-card consumer writes the card only when this is <c>>= Submitted</c> (a Draft yields no card). Additive:
    /// existing consumers (Notification) ignore it. Defaults to <c>Draft</c> for any legacy publisher that omits it.
    /// </summary>
    public ServiceRequestOfferStatus Status { get; set; } = ServiceRequestOfferStatus.Draft;
}
