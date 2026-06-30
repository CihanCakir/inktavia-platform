using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a subscription payment fails OR when an active subscription expires
/// without being renewed (SubscriptionRenewalJob transitions status to PastDue/Expired).
/// Consumers: Notification module (renewal reminder / expiry alert email).
///
/// ── FailureReason values ──────────────────────────────────────────────────────
///  "subscription_expired"        — period ended, AutoRenew=false; status → Expired
///  "auto_renewal_past_due"       — period ended, AutoRenew=true; status → PastDue (awaiting manual payment)
///  "gateway_payment_declined"    — active renewal attempt was declined by gateway
///  "iap_receipt_invalid"         — Apple/Google receipt validation failed (Phase 2C)
///
/// ── ProfileType values ────────────────────────────────────────────────────────
///  "Provider"    — ProviderPlanSubscription
///  "Participant" — ParticipantPlanSubscription
/// </summary>
public sealed class SubscriptionPaymentFailedMessage : AizenBaseMessage
{
    /// <summary>ProviderProfileId or ParticipantProfileId.</summary>
    public long    ProfileId        { get; init; }

    /// <summary>"Provider" or "Participant"</summary>
    public string  ProfileType      { get; init; } = default!;

    public long    PlanId           { get; init; }
    public string  PlanCode         { get; init; } = default!;
    public string  PlanName         { get; init; } = default!;

    public string  GatewayProvider  { get; init; } = "Iyzico";
    public string  FailureReason    { get; init; } = default!;

    /// <summary>
    /// True when this failure is triggered by SubscriptionRenewalJob for a past-due auto-renewal.
    /// False when the initial subscription payment itself failed.
    /// </summary>
    public bool    IsRenewalFailure { get; init; }

    /// <summary>New subscription status after this event: "PastDue" or "Expired".</summary>
    public string  NewStatus        { get; init; } = default!;

    public DateTime? PeriodEndedAt  { get; init; }
    public DateTime  FailedAtUtc    { get; init; }
}
