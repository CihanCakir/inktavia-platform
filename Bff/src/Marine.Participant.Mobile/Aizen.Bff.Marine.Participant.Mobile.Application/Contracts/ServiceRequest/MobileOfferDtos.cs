namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

// BE_MO2 — owner offers inbox + cost-free economics. COST-FREE: only the customer-facing totals + line items +
// S3 FX cross here — NEVER provider cost, commission, funding split, provider-net, or part-cost. Enums cross as
// their string names. Totals are in the platform settlement currency (TRY), never the stale USD default.

/// <summary>One provider offer received on the owner's SR — the compare-and-decide row + its breakdown.</summary>
public sealed class MobileServiceRequestOfferDto
{
    public long OfferId { get; set; }
    public long ServiceRequestId { get; set; }
    /// <summary>Offer status name (Submitted/UnderReview/Accepted/Rejected/Withdrawn/Expired).</summary>
    public string Status { get; set; } = default!;
    /// <summary>Provider display name. Null until a provider-name lookup exists — the FE shows a localized fallback.</summary>
    public string? ProviderName { get; set; }

    /// <summary>The settlement currency the totals are denominated in (TRY) — not the offer's stale USD default.</summary>
    public string CurrencyCode { get; set; } = "TRY";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    /// <summary>The customer total the owner would pay if they accept (settlement currency).</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>Phase-1 read-only display: distance (km) between the provider and the vessel, snapshot when the
    /// offer was created. Null when either location was unknown. Cost-free — a plain number, no coordinates.</summary>
    public decimal? DistanceKm { get; set; }

    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedEndDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Deposit type name (None/Percentage/FixedAmount…) + value (descriptive terms, cost-free).</summary>
    public string DepositType { get; set; } = default!;
    public decimal? DepositValue { get; set; }
    public string? PaymentTermsNote { get; set; }
    public string? WarrantyNote { get; set; }
    public string? Description { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>True while the offer is live for the owner to accept (MO3) or reject.</summary>
    public bool IsActionable { get; set; }

    public List<MobileServiceRequestOfferItemDto> Items { get; set; } = new();
}

/// <summary>One cost-free offer line — customer-facing figures + S3 FX transparency only.</summary>
public sealed class MobileServiceRequestOfferItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    /// <summary>Line item type name (Service/Product/Installation/Inspection/Repair/Emergency/Travel/Other).</summary>
    public string ItemType { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string? UnitCode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    // ── BE-S3 FX transparency (descriptive): the source figure + the converted settlement figure ──
    /// <summary>The currency <see cref="UnitPrice"/> is in (settlement, TRY) — always the settlement currency.</summary>
    public string SettlementCurrencyCode { get; set; } = "TRY";
    /// <summary>Raw foreign unit price before conversion; null ⇒ the line was quoted natively in TRY.</summary>
    public decimal? SourceUnitPrice { get; set; }
    /// <summary>The source currency of <see cref="SourceUnitPrice"/> (e.g. "EUR"); null when not converted.</summary>
    public string? SourceCurrencyCode { get; set; }
}

/// <summary>Reject payload — the N-E structured reason (enum name) + an optional free-text note. No money (MO3).</summary>
public sealed class RejectMobileOfferRequest
{
    /// <summary>OfferRejectReason name (PriceTooHigh/ChoseAnotherOffer/ScopeMismatch/Timing/Other). Blank → Other.</summary>
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }
}

// ── BE_MO3 — owner accept + pay + status ─────────────────────────────────────────────────────────────
// The accept body carries NOTHING the client controls about money — the offer is identified by the route, and the
// module recomputes the economics server-side (BE-P8). Amounts/provider ids are never client-supplied. COST-FREE:
// the owner sees the customer total (what they pay) + lifecycle status only — never commission / net / funding.

/// <summary>
/// The result of accepting an offer: the accepted offer + the SR's payment status right after accept. Because the
/// model captures at accept (P8 escrow + capture in one transactional step via the manual/iyzico gateway), on the
/// dev/manual gateway <see cref="Payment"/> is already <c>Paid</c>; the FE can still poll payment-status for the
/// live iyzico path (3DS is asynchronous). A blocked accept (Rejected/ConfigError) throws — no half-accepted SR.
/// </summary>
public sealed class MobileAcceptOfferResultDto
{
    public long OfferId { get; set; }
    public long ServiceRequestId { get; set; }
    /// <summary>True once the module committed the acceptance (offer → Accepted, escrow created).</summary>
    public bool Accepted { get; set; }
    /// <summary>The SR's payment status read straight after accept (drives the success/fail screen without a poll).</summary>
    public MobilePaymentStatusDto Payment { get; set; } = new();
}

/// <summary>
/// Owner-facing payment status of an accepted SR. COST-FREE: the customer total (what the owner paid) + a stable
/// lifecycle status only — never commission / net-payout / funding. <see cref="Status"/> is the owner lifecycle
/// (None / Pending / Paid / Failed / Cancelled); <see cref="IsPaid"/>/<see cref="IsPending"/>/<see cref="IsFailed"/>
/// are convenience flags. Drives the accept→pay→result flow and the retry decision on failure.
/// </summary>
public sealed class MobilePaymentStatusDto
{
    public long ServiceRequestId { get; set; }
    /// <summary>False before an offer is accepted (no escrow transaction yet).</summary>
    public bool HasPayment { get; set; }
    public long? TransactionId { get; set; }
    /// <summary>Owner lifecycle: None / Pending / Paid / Failed / Cancelled.</summary>
    public string Status { get; set; } = "None";
    /// <summary>Customer total — what the owner pays (settlement currency). Cost-free.</summary>
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? PaidAt { get; set; }

    public bool IsPaid    => string.Equals(Status, "Paid", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);
    /// <summary>Failed OR Cancelled — a retriable terminal state (the accept is not left dangling unpaid).</summary>
    public bool IsFailed  => string.Equals(Status, "Failed", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
}
