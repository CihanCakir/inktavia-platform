using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

[DocumentationInfo("ServiceRequest offer entity", "An offer from a service provider responding to a service request.")]
public sealed class ServiceRequestOfferEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ProviderProfileId { get; private set; }
    public long ProviderUserId { get; private set; }
    public ServiceRequestOfferStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string? ProviderNotes { get; private set; }
    public DateTime? EstimatedStartDate { get; private set; }
    public DateTime? EstimatedEndDate { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    /// <summary>Free-text reject note (N-E: the ReasonNote; the structured reason lives in <see cref="RejectReasonCode"/>).</summary>
    public string? RejectionReason { get; private set; }
    /// <summary>N-E structured reject reason (owner). Null for pre-taxonomy rows (backfilled to Other).</summary>
    public OfferRejectReason? RejectReasonCode { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string? WithdrawalReason { get; private set; }

    // --- Totals (computed by calculation service in 10c) ---
    // TotalAmount is kept as the legacy alias for GrandTotal — both always agree.
    public decimal TotalAmount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal GrandTotal { get; private set; }

    // Per-category totals (computed by calculation service)
    public decimal ServiceTotal { get; private set; }
    public decimal ProductTotal { get; private set; }
    public decimal LaborTotal { get; private set; }
    public decimal InstallationTotal { get; private set; }
    public decimal InspectionTotal { get; private set; }
    public decimal DeliveryTotal { get; private set; }
    public decimal EmergencyFeeTotal { get; private set; }
    public decimal OtherTotal { get; private set; }

    // ── Line economics aggregate (BE-S1) ──────────────────────────────────────
    /// <summary>
    /// Σ of each priced line's <c>CommissionBaseAmount</c> (§20.15 line→aggregate). The eligible base the
    /// transaction-level commission (S7/P8) reconciles against. Invariant: <c>CommissionBaseTotal ≤ Subtotal</c>.
    /// </summary>
    public decimal CommissionBaseTotal { get; private set; }

    // ── Customer discount funding aggregate (BE-S6) — computed; 0 unless a customer discount is applied ──
    public decimal TotalCustomerDiscount       { get; private set; }
    public decimal TotalPlatformFundedDiscount { get; private set; }
    public decimal TotalProviderFundedDiscount { get; private set; }

    // Commercial terms
    public OfferDepositType DepositType { get; private set; }
    public decimal? DepositValue { get; private set; }
    public string? PaymentTermsNote { get; private set; }
    public string? WarrantyNote { get; private set; }

    // Lifecycle timestamps
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ViewedAt { get; private set; }
    public DateTime? RevisionRequestedAt { get; private set; }

    private readonly List<ServiceRequestOfferItemEntity> _items = new();
    public IReadOnlyCollection<ServiceRequestOfferItemEntity> Items => _items.AsReadOnly();

    // ── BE-S3b — offer-level FX rate snapshots (one row per non-TRY source currency), written at submit ──
    private readonly List<OfferFxSnapshotEntity> _fxSnapshots = new();
    public IReadOnlyCollection<OfferFxSnapshotEntity> FxSnapshots => _fxSnapshots.AsReadOnly();

    public ServiceRequestOfferEntity() { }

    public static ServiceRequestOfferEntity Create(
        long serviceRequestId,
        long providerProfileId,
        long providerUserId,
        decimal totalAmount,
        string currencyCode,
        string? description,
        string? providerNotes,
        DateTime? estimatedStartDate,
        DateTime? estimatedEndDate,
        int? estimatedDurationMinutes,
        DateTime? expiresAt)
    {
        return new ServiceRequestOfferEntity
        {
            ServiceRequestId = serviceRequestId,
            ProviderProfileId = providerProfileId,
            ProviderUserId = providerUserId,
            Status = ServiceRequestOfferStatus.Draft,
            TotalAmount = totalAmount,
            GrandTotal = totalAmount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Description = description,
            ProviderNotes = providerNotes,
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public void MarkSubmitted(DateTime utcNow)
    {
        if (Status != ServiceRequestOfferStatus.Draft)
            throw new InvalidOperationException($"Cannot submit an offer in status {Status}.");
        Status = ServiceRequestOfferStatus.Submitted;
        SubmittedAt = utcNow;
    }

    public void Submit() => MarkSubmitted(DateTime.UtcNow);

    public void MarkViewed(DateTime utcNow)
    {
        ViewedAt ??= utcNow;
    }

    public void MarkRevisionRequested(DateTime utcNow)
    {
        RevisionRequestedAt = utcNow;
    }

    public void Accept()
    {
        Status = ServiceRequestOfferStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason, OfferRejectReason? reasonCode = null)
    {
        Status = ServiceRequestOfferStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
        RejectReasonCode = reasonCode;
    }

    public void Withdraw(string? reason)
    {
        Status = ServiceRequestOfferStatus.Withdrawn;
        WithdrawnAt = DateTime.UtcNow;
        WithdrawalReason = reason;
    }

    public void Update(
        decimal totalAmount,
        string currencyCode,
        string? description,
        string? providerNotes,
        DateTime? estimatedStartDate,
        DateTime? estimatedEndDate,
        int? estimatedDurationMinutes,
        DateTime? expiresAt)
    {
        if (Status != ServiceRequestOfferStatus.Draft)
            throw new InvalidOperationException($"Cannot edit an offer in status {Status}. Only Draft offers are editable.");
        TotalAmount = totalAmount;
        GrandTotal = totalAmount;
        CurrencyCode = currencyCode.ToUpperInvariant();
        Description = description;
        ProviderNotes = providerNotes;
        EstimatedStartDate = estimatedStartDate;
        EstimatedEndDate = estimatedEndDate;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        ExpiresAt = expiresAt;
    }

    public void UpdateCommercialTerms(
        OfferDepositType depositType,
        decimal? depositValue,
        string? paymentTermsNote,
        string? warrantyNote)
    {
        DepositType = depositType;
        DepositValue = depositValue;
        PaymentTermsNote = paymentTermsNote;
        WarrantyNote = warrantyNote;
    }

    /// <summary>
    /// Atomically replaces the item set. Clears existing items and adds the new set.
    /// Only allowed on Draft offers.
    /// </summary>
    public void ReplaceItems(IEnumerable<ServiceRequestOfferItemEntity> newItems)
    {
        if (Status != ServiceRequestOfferStatus.Draft)
            throw new InvalidOperationException($"Cannot modify items on an offer in status {Status}.");
        _items.Clear();
        _items.AddRange(newItems);
    }

    /// <summary>
    /// Sets the computed totals. Called by the calculation service only.
    /// TotalAmount is kept in sync with GrandTotal.
    /// </summary>
    public void SetComputedTotals(
        decimal subtotal, decimal discountTotal, decimal taxTotal, decimal grandTotal,
        decimal serviceTotal, decimal productTotal, decimal laborTotal, decimal installationTotal,
        decimal inspectionTotal, decimal deliveryTotal, decimal emergencyFeeTotal, decimal otherTotal)
    {
        Subtotal = subtotal;
        DiscountTotal = discountTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
        TotalAmount = grandTotal; // keep legacy alias in sync

        ServiceTotal = serviceTotal;
        ProductTotal = productTotal;
        LaborTotal = laborTotal;
        InstallationTotal = installationTotal;
        InspectionTotal = inspectionTotal;
        DeliveryTotal = deliveryTotal;
        EmergencyFeeTotal = emergencyFeeTotal;
        OtherTotal = otherTotal;
    }

    /// <summary>Sets the aggregate commission base (BE-S1). Called by the calculation service only.</summary>
    public void SetCommissionBaseTotal(decimal commissionBaseTotal)
    {
        CommissionBaseTotal = commissionBaseTotal;
    }

    /// <summary>BE-S6 — sets the aggregate customer discount + funding split. Called by the calculation service only.</summary>
    public void SetCustomerDiscountTotals(decimal totalCustomerDiscount, decimal totalPlatformFunded, decimal totalProviderFunded)
    {
        TotalCustomerDiscount       = totalCustomerDiscount;
        TotalPlatformFundedDiscount = totalPlatformFunded;
        TotalProviderFundedDiscount = totalProviderFunded;
    }

    public void AddItem(ServiceRequestOfferItemEntity item) => _items.Add(item);
    public void RecalculateTotal() => TotalAmount = _items.Sum(i => i.Quantity * i.UnitPrice);

    /// <summary>
    /// BE-S3b — atomically replaces the offer's FX rate snapshots (Draft-only; called by the FX resolver at submit). A
    /// re-submit re-resolves and replaces; after acceptance the offer can no longer be Draft, so the rows are frozen.
    /// </summary>
    public void ReplaceFxSnapshots(IEnumerable<OfferFxSnapshotEntity> snapshots)
    {
        if (Status != ServiceRequestOfferStatus.Draft)
            throw new InvalidOperationException($"Cannot modify FX snapshots on an offer in status {Status}.");
        _fxSnapshots.Clear();
        _fxSnapshots.AddRange(snapshots);
    }
}
