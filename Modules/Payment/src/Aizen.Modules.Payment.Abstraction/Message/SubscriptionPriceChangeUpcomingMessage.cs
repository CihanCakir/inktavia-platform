using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// N1 (§13.2) — published by the daily <c>SubscriptionPriceChangeReminderJob</c> when an active auto-renewing provider
/// subscription's resolved renewal price differs from the current, within the lead window. The Notification module
/// reminds the provider (N-B path). One message per upcoming price-version (idempotency via the subscription's
/// <c>PriceChangeReminderVersionKey</c> marker). <c>ProviderProfileId</c> is the notification recipient key, matching
/// the existing Payment-notification convention.
/// </summary>
[DocumentationInfo("Subscription price-change upcoming message", "A provider subscription's renewal price changes soon (N1).")]
public sealed class SubscriptionPriceChangeUpcomingMessage : AizenBaseMessage
{
    public long     SubscriptionId    { get; init; }
    public long     ProviderProfileId { get; init; }
    public string   PlanCode          { get; init; } = default!;
    public decimal  CurrentPrice      { get; init; }
    public decimal  NewPrice          { get; init; }
    public string   CurrencyCode      { get; init; } = "TRY";
    public DateTime EffectiveAtUtc    { get; init; }
}
