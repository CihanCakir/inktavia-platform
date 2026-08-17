using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Kit entity",
    "The digital twin of a physical moisture protection kit. " +
    "Status transitions are enforced through domain methods only. " +
    "EfficiencyPercent and DaysUntilExpiry are computed properties — NOT mapped to the database. " +
    "Phase 0 (July 2026): Added commercial foundation fields — SalesChannel, CommercialModel, " +
    "ProviderProfileId, StockLocationType, InvoiceId, PaymentTransactionId. " +
    "Phase 0 addendum (July 2026): Added WarehouseId — cross-module reference to future Warehouse entity.")]
public sealed class CargoDryKitEntity : AizenEntityWithAudit
{
    // ── Core identity ──────────────────────────────────────────────────────────
    public string            SerialNumber   { get; private set; } = default!;
    public string            KitCode        { get; private set; } = default!;
    public string            QrPayload      { get; private set; } = default!;
    public string            ProductCode    { get; private set; } = default!;
    public string            BatchCode      { get; private set; } = default!;
    public CargoDryKitStatus Status         { get; private set; }
    public long?             OwnerUserId    { get; private set; }
    public long?             VesselId       { get; private set; }
    public DateTimeOffset    ManufacturedAt { get; private set; }
    public DateTimeOffset?   ActivatedAt    { get; private set; }
    public DateTimeOffset?   ExpiresAt      { get; private set; }
    public int               RenewalCount   { get; private set; }
    public string?           RevokeReason   { get; private set; }
    public DateTimeOffset?   RevokedAt      { get; private set; }

    // ── Commercial foundation (Phase 0, July 2026) ────────────────────────────
    /// <summary>
    /// Which provider sold or facilitated this kit sale.
    /// Null = no provider attribution (DirectSale or unattributed).
    /// Retained on renewal for analytics (Decision N12).
    /// </summary>
    public long?                    ProviderProfileId { get; private set; }

    /// <summary>
    /// How this kit reached the end user.
    /// Null = not yet determined; triggers CommercialReviewRequired on activation (Decision N18).
    /// Default after batch generation: stays null until batch is allocated.
    /// </summary>
    public SalesChannel?            SalesChannel      { get; private set; }

    /// <summary>
    /// Commercial model that applies to this kit's revenue recognition.
    /// PrincipalSale = Inktavia earns directly; MarketplaceCommission = provider earns payout.
    /// </summary>
    public CargoDryCommercialModel? CommercialModel   { get; private set; }

    /// <summary>
    /// Physical/logical location of the kit in the inventory chain.
    /// PlatformWarehouse = default after generation.
    /// ProviderWarehouse = after batch allocation to provider.
    /// Activated = after kit is activated on a vessel.
    /// </summary>
    public StockLocationType        StockLocationType { get; private set; } = StockLocationType.PlatformWarehouse;

    /// <summary>
    /// Cross-module reference to InvoiceHeaderEntity in the Payment module.
    /// No EF FK constraint — referenced by Id only.
    /// Set when the kit purchase invoice is issued.
    /// </summary>
    public long? InvoiceId            { get; private set; }

    /// <summary>
    /// Cross-module reference to PaymentTransactionEntity in the Payment module.
    /// No EF FK constraint — referenced by Id only.
    /// </summary>
    public long? PaymentTransactionId { get; private set; }

    /// <summary>
    /// Cross-module reference to the future WarehouseEntity (Phase 6).
    /// Null = no specific warehouse assigned (platform stock / activated kits).
    /// Set when a batch is allocated to a provider warehouse.
    /// No EF FK constraint — referenced by Id only.
    /// </summary>
    public long? WarehouseId { get; private set; }

    /// <summary>
    /// Reference to the CargoDryConsignmentAgreementEntity that governs this kit's sell-through settlement.
    /// Null when the kit is not under a consignment agreement (e.g. DirectSale or MarketplaceCommission).
    /// </summary>
    public long? ConsignmentAgreementId { get; private set; }

    private CargoDryKitEntity() { }

    // ── Factory ────────────────────────────────────────────────────────────────
    public static CargoDryKitEntity Create(
        string serialNumber, string kitCode, string qrPayload,
        string productCode, string batchCode)
        => new()
        {
            SerialNumber      = serialNumber,
            KitCode           = kitCode,
            QrPayload         = qrPayload,
            ProductCode       = productCode,
            BatchCode         = batchCode,
            Status            = CargoDryKitStatus.Available,
            StockLocationType = StockLocationType.PlatformWarehouse,
            ManufacturedAt    = DateTimeOffset.UtcNow,
            IsActive          = true,
        };

    // ── Lifecycle methods ──────────────────────────────────────────────────────
    public void Activate(long userId, long vesselId, int validityDays)
    {
        if (Status != CargoDryKitStatus.Available && Status != CargoDryKitStatus.CommercialReviewRequired)
            throw new InvalidOperationException(
                $"Kit {SerialNumber} cannot be activated — current status: {Status}");

        // Decision N18/N19: if SalesChannel is not set, mark for commercial review
        // and do NOT complete activation — caller must handle this return path.
        if (SalesChannel == null)
        {
            Status = CargoDryKitStatus.CommercialReviewRequired;
            return;
        }

        Status            = CargoDryKitStatus.Activated;
        StockLocationType = StockLocationType.Activated;
        OwnerUserId       = userId;
        VesselId          = vesselId;
        ActivatedAt       = DateTimeOffset.UtcNow;
        ExpiresAt         = DateTimeOffset.UtcNow.AddDays(validityDays);
    }

    public void Renew(int additionalDays, string paymentRef)
    {
        if (Status != CargoDryKitStatus.Activated && Status != CargoDryKitStatus.Expired)
            throw new InvalidOperationException($"Kit {SerialNumber} cannot be renewed — status: {Status}");

        var baseDate  = ExpiresAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow
            ? ExpiresAt.Value
            : DateTimeOffset.UtcNow;
        ExpiresAt     = baseDate.AddDays(additionalDays);
        Status        = CargoDryKitStatus.Activated;
        RenewalCount += 1;
        // Decision N10/N12: renewal does not change ProviderProfileId or SalesChannel
        _ = paymentRef;
    }

    public void MarkExpired()
    {
        if (Status == CargoDryKitStatus.Activated)
            Status = CargoDryKitStatus.Expired;
    }

    public void Revoke(string reason)
    {
        Status       = CargoDryKitStatus.Revoked;
        RevokeReason = reason;
        RevokedAt    = DateTimeOffset.UtcNow;
    }

    public void Transfer(long newUserId, long newVesselId)
    {
        if (Status != CargoDryKitStatus.Activated)
            throw new InvalidOperationException("Only active kits can be transferred.");
        OwnerUserId = newUserId;
        VesselId    = newVesselId;
    }

    // ── Commercial assignment methods (Phase 0) ────────────────────────────────

    /// <summary>
    /// Assign this kit to a provider with a specific commercial model.
    /// Called when a batch is allocated to a provider (Phase 1 AllocateBatchToProviderCommand).
    /// warehouseId: optional — set when the provider has a registered warehouse (Phase 6 FK; safe as nullable now).
    /// </summary>
    public void AssignToProvider(
        long providerProfileId,
        SalesChannel channel,
        CargoDryCommercialModel model,
        long? warehouseId = null)
    {
        ProviderProfileId = providerProfileId;
        SalesChannel      = channel;
        CommercialModel   = model;
        WarehouseId       = warehouseId;
        StockLocationType = StockLocationType.ProviderWarehouse;
    }

    /// <summary>
    /// Updates the warehouse and stock location independently of commercial attribution.
    /// Used for stock movements (e.g., transfer between warehouses).
    /// </summary>
    public void AssignWarehouse(long? warehouseId, StockLocationType stockLocationType)
    {
        WarehouseId       = warehouseId;
        StockLocationType = stockLocationType;
    }

    /// <summary>
    /// Sets SalesChannel for direct platform sales (no provider).
    /// </summary>
    public void MarkAsDirectSale()
    {
        SalesChannel    = Aizen.Modules.CargoDry.Abstraction.Enum.SalesChannel.DirectSale;
        CommercialModel = CargoDryCommercialModel.PrincipalSale;
    }

    /// <summary>
    /// Links this kit to its payment transaction and invoice (cross-module, Id-only references).
    /// </summary>
    public void LinkPayment(long transactionId, long invoiceId)
    {
        PaymentTransactionId = transactionId;
        InvoiceId            = invoiceId;
    }

    /// <summary>
    /// Admin resolves commercial attribution after CommercialReviewRequired status.
    /// Completes the activation that was deferred.
    /// </summary>
    public void ResolveCommercialAttribution(
        SalesChannel channel, CargoDryCommercialModel model, long? providerProfileId,
        long userId, long vesselId, int validityDays)
    {
        if (Status != CargoDryKitStatus.CommercialReviewRequired)
            throw new InvalidOperationException($"Kit {SerialNumber} is not in CommercialReviewRequired status.");

        SalesChannel      = channel;
        CommercialModel   = model;
        ProviderProfileId = providerProfileId;
        Status            = CargoDryKitStatus.Activated;
        StockLocationType = StockLocationType.Activated;
        OwnerUserId       = userId;
        VesselId          = vesselId;
        ActivatedAt       ??= DateTimeOffset.UtcNow;
        ExpiresAt         = DateTimeOffset.UtcNow.AddDays(validityDays);
    }

    // ── Computed properties (NOT mapped to DB) ─────────────────────────────────
    public double EfficiencyPercent
    {
        get
        {
            if (!ExpiresAt.HasValue || !ActivatedAt.HasValue) return 0;
            var total     = (ExpiresAt.Value - ActivatedAt.Value).TotalDays;
            var remaining = (ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
            return total <= 0 ? 0 : Math.Max(0, Math.Min(100, remaining / total * 100));
        }
    }

    public int DaysUntilExpiry
        => ExpiresAt.HasValue
            ? (int)Math.Max(0, Math.Ceiling((ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays))
            : 0;

    public bool IsExpiringSoon(int withinDays = 30)
        => Status == CargoDryKitStatus.Activated && DaysUntilExpiry <= withinDays;
}
