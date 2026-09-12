using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// BE_WC1 — published when a provider SUBMITS an offer (Draft → Submitted). A first-class lifecycle event dedicated to
/// the thread's **offer card**, so the Messaging module can generate it independently of the chat-mirror event (which
/// WC4 removes). Distinct from <c>ServiceRequestOfferCreatedMessage</c> (published by the older create-and-submit path
/// and consumed by the Notification module) — the two paths are mutually exclusive per offer, so a single offer can
/// never fire both events. Consumed by the Messaging offer-card consumer AND (this fix) the Notification module, which
/// notifies the provider + owner for the draft→submit path the create event never covered. Carries the final
/// submit-time total so the card reads <c>offer:{offerId}|{TotalAmount:F2} {CurrencyCode}</c>.
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

    /// <summary>
    /// BE_NF1 (D2) — the owning requester's user id, so the Notification module can notify the owner (not only the
    /// provider) when an offer lands. Additive; defaults to 0 for any legacy publisher that omits it (owner then skipped).
    /// </summary>
    public long OwnerUserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public DateTimeOffset OccurredAt { get; set; }
}
