using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// N4 (§9) — published fire-and-forget by <c>PremiumBoostService.OnBoostRefundedAsync</c> when an Active premium boost
/// entitlement is revoked on refund (once — only on the actual Active→Revoked transition). The Notification module tells
/// the provider their boost was cancelled.
/// </summary>
[DocumentationInfo("Premium boost revoked message", "A premium boost entitlement was revoked on refund (N4).")]
public sealed class PremiumBoostRevokedMessage : AizenBaseMessage
{
    public long   ProviderProfileId { get; init; }
    public long   OfferId           { get; init; }
    public long   EntitlementId     { get; init; }
    public string Reason            { get; init; } = default!;
}
