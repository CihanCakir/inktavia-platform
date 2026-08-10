namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;

// BE_MO7 — owner participant membership. The owner views active plans, their current subscription, subscribes
// (free = immediate; paid = iyzico-gated checkout) and cancels. COST-FREE: only the plan's customer price + the
// advertised perks (discount rate, earn multiplier, features) cross — never platform/provider funding internals.
// The plan DTO field names mirror the Payment ParticipantPlanDto so Refit maps the module response directly; the
// non-display fields (IsActive/SortOrder/ValidFrom/ValidTo) are simply not declared → dropped.

/// <summary>One advertised plan feature line.</summary>
public sealed class MobilePlanFeatureDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
}

/// <summary>An available participant plan (cost-free — customer price + perks only).</summary>
public sealed class MobileMembershipPlanDto
{
    public long Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Monthly customer price (₺). 0 for a free/launch plan.</summary>
    public decimal MonthlyPriceTRY { get; set; }
    public decimal? AnnualPriceTRY { get; set; }
    public int? TrialDays { get; set; }
    public string? BadgeLabel { get; set; }
    /// <summary>The customer service-discount rate this plan earns (e.g. 0.10 = 10% off service offers).</summary>
    public decimal ServiceDiscountRate { get; set; }
    public decimal CargoDryDiscountRate { get; set; }
    public decimal InkCoinEarnMultiplier { get; set; }
    /// <summary>True when the plan is free (₺0) — subscribing is immediate, no payment.</summary>
    public bool IsFree { get; set; }
    public List<MobilePlanFeatureDto> FeatureItems { get; set; } = new();
}

/// <summary>The caller's current active subscription (cost-free). Null-returned when they have no plan.</summary>
public sealed class MobileCurrentSubscriptionDto
{
    public long SubscriptionId { get; set; }
    public long ParticipantPlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    /// <summary>SubscriptionStatus name (Active/PastDue/Cancelled/Expired).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>The customer price paid at subscription (₺). Cost-free.</summary>
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public DateTime PeriodStart { get; set; }
    /// <summary>End of the current billing period (the renewal / next-charge date when AutoRenew).</summary>
    public DateTime PeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
    /// <summary>The service-discount rate locked in at subscription — the plan benefit the owner earns.</summary>
    public decimal ServiceDiscountRate { get; set; }
    public decimal EarnMultiplier { get; set; }
}

/// <summary>Subscribe payload — only the chosen plan id (the price/participant are server-resolved).</summary>
public sealed class MobileSubscribeRequest
{
    public long PlanId { get; set; }
}

/// <summary>The result of subscribing. For a free plan <see cref="Mode"/> = <c>Immediate</c> and the subscription is
/// active now (<see cref="Payment"/> = None). For a paid plan <see cref="Mode"/> = <c>PaymentPending</c>,
/// <see cref="TransactionId"/> is set, and <see cref="Payment"/> reflects the checkout (Pending on init; Paid once
/// captured — poll the payment-status endpoint). Cost-free.</summary>
public sealed class MobileSubscribeResultDto
{
    /// <summary>"Immediate" (free applied now) | "PaymentPending" (paid, awaiting capture).</summary>
    public string Mode { get; set; } = "Immediate";
    public long ParticipantPlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public long? SubscriptionId { get; set; }
    public long? TransactionId { get; set; }
    /// <summary>The subscription's payment lifecycle (drives the pay flow / poll on the paid path).</summary>
    public MobileMembershipPaymentStatusDto Payment { get; set; } = new();
}

/// <summary>Owner-facing subscription payment status (cost-free) — mirrors the MO3 payment-status shape so the FE
/// reuses the same poll.</summary>
public sealed class MobileMembershipPaymentStatusDto
{
    public bool HasPayment { get; set; }
    public long? TransactionId { get; set; }
    /// <summary>None / Pending / Paid / Failed / Cancelled.</summary>
    public string Status { get; set; } = "None";
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? PaidAt { get; set; }

    public bool IsPaid => string.Equals(Status, "Paid", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);
    public bool IsFailed => string.Equals(Status, "Failed", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
}

/// <summary>The result of cancelling (cost-free).</summary>
public sealed class MobileCancelSubscriptionDto
{
    public long SubscriptionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}
