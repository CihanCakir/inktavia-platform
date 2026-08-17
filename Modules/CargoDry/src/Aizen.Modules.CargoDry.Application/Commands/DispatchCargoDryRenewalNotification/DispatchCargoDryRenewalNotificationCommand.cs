using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.DispatchCargoDryRenewalNotification;

/// <summary>
/// Publishes CargoDryRenewalNotificationRequestedMessage to the message bus.
/// Notification module consumer handles actual delivery.
/// CargoDry does NOT call SMS/Mail/Push providers directly.
/// Does NOT create PaymentTransaction.
/// Idempotent: if already dispatched, returns without re-publishing.
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("Dispatch CargoDry renewal notification command",
    "Publishes CargoDryRenewalNotificationRequestedMessage to message bus. " +
    "Notification module owns delivery. Idempotent. " +
    "Does NOT create PaymentTransaction. Does NOT call SMS/Mail directly. " +
    "Phase 11 (July 2026).")]
public sealed class DispatchCargoDryRenewalNotificationCommand
    : AizenCommand<CargoDryRenewalPreparationDto>
{
    public required long   RenewalPreparationId { get; init; }
    /// <summary>Recipient email — enriched by BFF from Identity module (optional).</summary>
    public string?         RecipientEmail       { get; init; }
    /// <summary>Recipient phone — enriched by BFF from Identity module (optional).</summary>
    public string?         RecipientPhone       { get; init; }
}
