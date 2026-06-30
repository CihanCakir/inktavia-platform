using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published after a subscription is successfully activated (payment confirmed + record created).
/// Consumers: Notification module (welcome/receipt email), Identity module (feature unlock).
///
/// ── GatewayProvider values ────────────────────────────────────────────────────
///  "Iyzico"          — web/card payment (Provider + Participant card fallback)
///  "AppleAppStore"   — iOS In-App Purchase (Participant only, Phase 2C)
///  "GooglePlayStore" — Android In-App Purchase (Participant only, Phase 2C)
///
/// ── ProfileType values ────────────────────────────────────────────────────────
///  "Provider"    — ProviderPlanSubscription
///  "Participant" — ParticipantPlanSubscription
///
/// Idempotency: consumers deduplicate on SUBSCRIPTION-SUCCEEDED-{SubscriptionId}.
/// </summary>
public sealed class SubscriptionPaymentSucceededMessage : AizenBaseMessage
{
    /// <summary>PK of ProviderPlanSubscriptionEntity or ParticipantPlanSubscriptionEntity.</summary>
    public long    SubscriptionId   { get; init; }

    /// <summary>ProviderProfileId or ParticipantProfileId — who holds the subscription.</summary>
    public long    ProfileId        { get; init; }

    /// <summary>"Provider" or "Participant"</summary>
    public string  ProfileType      { get; init; } = default!;

    public long    PlanId           { get; init; }
    public string  PlanCode         { get; init; } = default!;
    public string  PlanName         { get; init; } = default!;

    public decimal PaidAmount       { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";

    /// <summary>Payment gateway that processed the subscription fee.</summary>
    public string  GatewayProvider  { get; init; } = "Iyzico";

    public DateTime PeriodStart     { get; init; }
    public DateTime PeriodEnd       { get; init; }

    /// <summary>Null if invoice generation failed (consumer should not fail on this).</summary>
    public long?   InvoiceId        { get; init; }
    public string? InvoiceNumber    { get; init; }
}
