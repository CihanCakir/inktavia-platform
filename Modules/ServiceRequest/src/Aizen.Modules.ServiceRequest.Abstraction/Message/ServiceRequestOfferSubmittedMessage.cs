using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// BE_WC1 — published when a provider SUBMITS an offer (Draft → Submitted). A first-class lifecycle event dedicated to
/// the thread's **offer card**, so the Messaging module can generate it independently of the chat-mirror event (which
/// WC4 removes). Distinct from <c>ServiceRequestOfferCreatedMessage</c> (published at DRAFT creation and consumed by the
/// Notification module) — reusing that would fire at draft-time with no submit guarantee and double the offer
/// notification. This event is consumed ONLY by the Messaging offer-card consumer ⇒ no new notifications. Carries the
/// final submit-time total so the card reads <c>offer:{offerId}|{TotalAmount:F2} {CurrencyCode}</c>.
/// </summary>
[DocumentationInfo("Service request offer submitted message",
    "Published when a provider submits an offer — drives the offer-card message in Messaging (no notification).")]
public sealed class ServiceRequestOfferSubmittedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long ProviderProfileId { get; set; }
    /// <summary>The provider user that submitted (offer-card sender).</summary>
    public long ProviderUserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public DateTimeOffset OccurredAt { get; set; }
}
