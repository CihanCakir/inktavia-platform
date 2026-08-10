namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

// BE_MO6 — owner change orders (review + approve→incremental checkout / decrease-refund + reject). The provider
// proposes extra/removed work on an accepted SR; the owner reviews the changed lines + the incremental ₺, then
// approves (Increase → new incremental snapshot + capture-at-approve; Decrease → P10 refund of the delta) or rejects.
// COST-FREE (§20.13/§20.9): only customer-facing amounts + line inputs cross — never AppliedProviderNet, the economics
// snapshot / transaction / refund-record ids, the proposer's user id, or per-line commission/discount-eligibility
// flags. Enums cross as their string names.

/// <summary>One change-order line (offer-item-shaped, cost-free: the customer-facing inputs only).</summary>
public sealed class MobileChangeOrderItemDto
{
    /// <summary>ServiceRequestOfferItemType name (Labor/Part/Travel/…).</summary>
    public string ItemType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? UnitCode { get; set; }
    /// <summary>Tax rate as a fraction (e.g. 0.18). Descriptive only.</summary>
    public decimal TaxRate { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public int SortOrder { get; set; }
    /// <summary>Line estimate = round(Quantity·UnitPrice)·(1+TaxRate) — a pre-discount preview for the confirm sheet.
    /// The authoritative charged/refunded figure is the CO's <see cref="MobileChangeOrderDto.AppliedCustomerTotal"/>
    /// once applied.</summary>
    public decimal LineEstimate { get; set; }
}

/// <summary>A change order as the owner sees it (cost-free). Direction + status drive the review; the incremental ₺ is
/// what the owner would pay (Increase) or be refunded (Decrease).</summary>
public sealed class MobileChangeOrderDto
{
    public long ChangeOrderId { get; set; }
    public long ServiceRequestId { get; set; }
    public int SequenceNo { get; set; }
    /// <summary>ServiceChangeOrderStatus name (Proposed/CustomerApproved/Rejected/Applied/Cancelled).</summary>
    public string Status { get; set; } = default!;
    /// <summary>ServiceChangeOrderDirection name (Increase/Decrease).</summary>
    public string Direction { get; set; } = default!;
    public bool IsIncrease { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public string Reason { get; set; } = default!;
    public string? RejectionReason { get; set; }

    /// <summary>Pre-approve estimate (Σ line estimates, pre-discount) — what the owner would pay/be refunded. Preview
    /// only; superseded by <see cref="AppliedCustomerTotal"/> once applied.</summary>
    public decimal EstimatedAmount { get; set; }
    /// <summary>The applied customer-facing amount (charged for Increase, refunded for Decrease). 0 until Applied.</summary>
    public decimal AppliedCustomerTotal { get; set; }
    /// <summary>Signed contribution to the SR effective total (+Increase / −Decrease / 0 unless Applied).</summary>
    public decimal EffectiveTotalDelta { get; set; }

    /// <summary>Still awaiting the owner's decision (drives the approve/reject CTAs).</summary>
    public bool IsPending { get; set; }
    /// <summary>Terminal-applied (Increase captured / Decrease refunded).</summary>
    public bool IsApplied { get; set; }
    /// <summary>Terminal-rejected (owner rejected, or the incremental economics breached the P5/S9 gate).</summary>
    public bool IsRejected { get; set; }

    public DateTime ProposedAt { get; set; }
    public DateTime? CustomerApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? AppliedAt { get; set; }

    public List<MobileChangeOrderItemDto> Items { get; set; } = new();
}

/// <summary>The SR's change orders + the DERIVED effective total (original acceptance + Σ applied). Cost-free.</summary>
public sealed class MobileChangeOrderListDto
{
    public long ServiceRequestId { get; set; }
    /// <summary>The accepted offer's grand total (original acceptance amount).</summary>
    public decimal OriginalTotal { get; set; }
    /// <summary>Σ applied change-order deltas (+Increase, −Decrease).</summary>
    public decimal AppliedDelta { get; set; }
    /// <summary>OriginalTotal + AppliedDelta — the SR's current effective total.</summary>
    public decimal EffectiveTotal { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    /// <summary>Count of change orders still awaiting the owner's decision (Proposed).</summary>
    public int PendingCount { get; set; }
    public List<MobileChangeOrderDto> ChangeOrders { get; set; } = new();
}

/// <summary>Reject payload — an optional free-text reason. Rejecting is terminal; no economics.</summary>
public sealed class MobileRejectChangeOrderRequest
{
    public string? Reason { get; set; }
}

/// <summary>The result of approving a change order: the applied CO + the incremental payment status. For an Increase
/// on the dev/manual gateway the capture is synchronous, so <see cref="Payment"/> is already <c>Paid</c>; the FE can
/// still poll for the live-iyzico path. For a Decrease the refund is applied and <see cref="Payment"/> reflects the
/// (original) transaction the refund hit — the owner reads the refunded amount from the CO's AppliedCustomerTotal.</summary>
public sealed class MobileChangeOrderApproveResultDto
{
    public MobileChangeOrderDto ChangeOrder { get; set; } = new();
    public MobilePaymentStatusDto Payment { get; set; } = new();
}
