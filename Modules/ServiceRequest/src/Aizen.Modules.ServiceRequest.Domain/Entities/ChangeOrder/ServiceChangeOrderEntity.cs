using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;

/// <summary>
/// BE-S11b — a post-acceptance change order (§20.13): the ONLY way extra/removed work enters the SR's effective total after
/// acceptance, and only with <b>customer approval</b>. It NEVER mutates the accepted offer's economics snapshot. On apply an
/// <see cref="ServiceChangeOrderDirection.Increase"/> order produces a <b>new, additional</b> immutable economics snapshot +
/// a new escrow/split (P8/P9) for the incremental amount; a <see cref="ServiceChangeOrderDirection.Decrease"/> order issues a
/// P10 refund against the original transaction. Idempotent per change order via the Payment context ref
/// <c>SR-{sr}-OFFER-{offer}-CO-{id}</c> — a re-apply never double-charges. The SR's effective total is derived
/// (original acceptance snapshot + Σ applied change orders), never stored on the original.
/// </summary>
public sealed class ServiceChangeOrderEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    /// <summary>The accepted offer this change order extends. The change order's economics inherit the offer's provider/customer/category.</summary>
    public long AcceptedOfferId { get; private set; }
    /// <summary>1-based sequence within the accepted SR/offer (assigned by the repository at propose time).</summary>
    public int SequenceNo { get; private set; }

    public ServiceChangeOrderStatus Status { get; private set; }
    public ServiceChangeOrderDirection Direction { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";

    public string Reason { get; private set; } = default!;
    public long ProposedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }

    // Lifecycle timestamps (UTC)
    public DateTime ProposedAt { get; private set; }
    public DateTime? CustomerApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? AppliedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // ── Applied economics references (set once, on apply; the accepted snapshot is never touched) ──
    /// <summary>Increase: the NEW immutable economics snapshot this change order produced (separate from acceptance).</summary>
    public long? EconomicsSnapshotId { get; private set; }
    /// <summary>Increase: the NEW incremental escrow transaction. Decrease: the original transaction the P10 refund hit.</summary>
    public long? PaymentTransactionId { get; private set; }
    /// <summary>Decrease: the P10 refund record id.</summary>
    public long? RefundRecordId { get; private set; }
    /// <summary>The incremental customer-facing amount (Increase: charged; Decrease: refunded). Derived from the offer calc, positive.</summary>
    public decimal AppliedCustomerTotal { get; private set; }
    /// <summary>Increase: the incremental provider net that was split. 0 for a Decrease.</summary>
    public decimal AppliedProviderNet { get; private set; }

    private readonly List<ServiceChangeOrderItemEntity> _items = new();
    public IReadOnlyCollection<ServiceChangeOrderItemEntity> Items => _items.AsReadOnly();

    public ServiceChangeOrderEntity() { }

    public static ServiceChangeOrderEntity Create(
        long serviceRequestId, long acceptedOfferId, int sequenceNo,
        ServiceChangeOrderDirection direction, string currencyCode, string reason,
        long proposedByUserId, IEnumerable<ServiceChangeOrderItemEntity> items, DateTime utcNow)
    {
        var co = new ServiceChangeOrderEntity
        {
            ServiceRequestId = serviceRequestId,
            AcceptedOfferId  = acceptedOfferId,
            SequenceNo       = sequenceNo,
            Status           = ServiceChangeOrderStatus.Proposed,
            Direction        = direction,
            CurrencyCode     = currencyCode.ToUpperInvariant(),
            Reason           = reason.Trim(),
            ProposedByUserId = proposedByUserId,
            ProposedAt       = utcNow,
            IsActive         = true,
        };
        co._items.AddRange(items);
        if (co._items.Count == 0)
            throw new InvalidOperationException("A change order must have at least one proposed line.");
        return co;
    }

    /// <summary>Customer approves the proposal (mirrors the N-E owner-action pattern; exercisable via API/bus). Proposed → CustomerApproved.</summary>
    public void MarkCustomerApproved(DateTime utcNow)
    {
        if (Status != ServiceChangeOrderStatus.Proposed)
            throw new InvalidOperationException($"Only a Proposed change order can be approved (was {Status}).");
        Status = ServiceChangeOrderStatus.CustomerApproved;
        CustomerApprovedAt = utcNow;
    }

    /// <summary>Rejected — terminal. From Proposed (customer rejects) or CustomerApproved (incremental economics breached P5/S9).</summary>
    public void Reject(string? reason, DateTime utcNow)
    {
        if (Status is not (ServiceChangeOrderStatus.Proposed or ServiceChangeOrderStatus.CustomerApproved))
            throw new InvalidOperationException($"Cannot reject a change order in status {Status}.");
        Status = ServiceChangeOrderStatus.Rejected;
        RejectedAt = utcNow;
        RejectionReason = reason;
    }

    /// <summary>Provider withdraws an unapproved proposal. Proposed → Cancelled (terminal).</summary>
    public void Cancel(string? reason, DateTime utcNow)
    {
        if (Status != ServiceChangeOrderStatus.Proposed)
            throw new InvalidOperationException($"Only a Proposed change order can be cancelled (was {Status}).");
        Status = ServiceChangeOrderStatus.Cancelled;
        CancelledAt = utcNow;
        RejectionReason = reason;
    }

    /// <summary>Increase apply — links the NEW snapshot + incremental escrow. CustomerApproved → Applied.</summary>
    public void MarkApplied(long economicsSnapshotId, long paymentTransactionId, decimal customerTotal, decimal providerNet, DateTime utcNow)
    {
        if (Status != ServiceChangeOrderStatus.CustomerApproved)
            throw new InvalidOperationException($"Only an approved change order can be applied (was {Status}).");
        EconomicsSnapshotId  = economicsSnapshotId;
        PaymentTransactionId = paymentTransactionId;
        AppliedCustomerTotal = customerTotal;
        AppliedProviderNet   = providerNet;
        Status               = ServiceChangeOrderStatus.Applied;
        AppliedAt            = utcNow;
    }

    /// <summary>Decrease apply — links the P10 refund against the original transaction. CustomerApproved → Applied.</summary>
    public void MarkAppliedAsReduction(long originalTransactionId, long? refundRecordId, decimal refundedAmount, DateTime utcNow)
    {
        if (Status != ServiceChangeOrderStatus.CustomerApproved)
            throw new InvalidOperationException($"Only an approved change order can be applied (was {Status}).");
        PaymentTransactionId = originalTransactionId;
        RefundRecordId       = refundRecordId;
        AppliedCustomerTotal = refundedAmount;
        AppliedProviderNet   = 0m;
        Status               = ServiceChangeOrderStatus.Applied;
        AppliedAt            = utcNow;
    }

    /// <summary>The signed contribution to the SR's effective total once Applied (+ for Increase, − for Decrease; 0 otherwise).</summary>
    public decimal EffectiveTotalDelta =>
        Status != ServiceChangeOrderStatus.Applied ? 0m
        : Direction == ServiceChangeOrderDirection.Increase ? AppliedCustomerTotal
        : -AppliedCustomerTotal;
}
