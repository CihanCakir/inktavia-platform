using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;

/// <summary>BE-S11b — a change order as seen by provider/owner/admin.</summary>
public sealed class ServiceChangeOrderDto
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long AcceptedOfferId { get; set; }
    public int SequenceNo { get; set; }
    public ServiceChangeOrderStatus Status { get; set; }
    public ServiceChangeOrderDirection Direction { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public string Reason { get; set; } = default!;
    public string? RejectionReason { get; set; }
    public long ProposedByUserId { get; set; }

    /// <summary>The applied customer-facing amount (charged for Increase, refunded for Decrease). 0 until Applied.</summary>
    public decimal AppliedCustomerTotal { get; set; }
    public decimal AppliedProviderNet { get; set; }
    /// <summary>Signed contribution to the SR effective total (+Increase / −Decrease / 0 unless Applied).</summary>
    public decimal EffectiveTotalDelta { get; set; }
    public long? EconomicsSnapshotId { get; set; }
    public long? PaymentTransactionId { get; set; }
    public long? RefundRecordId { get; set; }

    public DateTime ProposedAt { get; set; }
    public DateTime? CustomerApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<ServiceChangeOrderItemDto> Items { get; set; } = new();
}

/// <summary>BE-S11b — a proposed change-order line.</summary>
public sealed class ServiceChangeOrderItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public int SortOrder { get; set; }
    public string? UnitCode { get; set; }
    public decimal TaxRate { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public LineCommissionEligibility CommissionEligibility { get; set; }
    public LineDiscountEligibility LineDiscountEligibility { get; set; }
}

/// <summary>
/// BE-S11b — the SR's change-order view with the DERIVED effective total: original acceptance total + Σ applied change
/// orders. The original acceptance snapshot is never mutated; this total is computed, never stored on it.
/// </summary>
public sealed class ServiceChangeOrderListDto
{
    public long ServiceRequestId { get; set; }
    /// <summary>The accepted offer's grand total (the original acceptance amount).</summary>
    public decimal OriginalTotal { get; set; }
    /// <summary>Σ of applied change-order deltas (+Increase, −Decrease).</summary>
    public decimal AppliedDelta { get; set; }
    /// <summary>OriginalTotal + AppliedDelta.</summary>
    public decimal EffectiveTotal { get; set; }
    public List<ServiceChangeOrderDto> ChangeOrders { get; set; } = new();
}
