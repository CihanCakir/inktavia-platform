using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// N4 (§9) — published fire-and-forget by <c>PremiumBoostService.OnBoostPaidAsync</c> after a premium boost entitlement
/// is created + Activated (once — the idempotent webhook guards duplicates). The Notification module tells the provider
/// their offer boost is active. <c>ProviderProfileId</c> is the notification recipient key.
/// </summary>
[DocumentationInfo("Premium boost activated message", "A premium boost entitlement was activated (N4).")]
public sealed class PremiumBoostActivatedMessage : AizenBaseMessage
{
    public long     ProviderProfileId { get; init; }
    public long     OfferId           { get; init; }
    public long     EntitlementId     { get; init; }
    public DateTime ExpiresAtUtc      { get; init; }
}
