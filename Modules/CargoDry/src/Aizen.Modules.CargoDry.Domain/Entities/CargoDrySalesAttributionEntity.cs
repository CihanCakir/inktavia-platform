using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Sales Attribution entity",
    "Created at kit activation to record the commercial attribution path for each kit. " +
    "One record per activated kit. " +
    "Tracks which sales channel and commercial model the kit belongs to, " +
    "the provider who sold it (if any), commission amounts, and settlement linkage. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class CargoDrySalesAttributionEntity : AizenEntityWithAudit
{
    // ── Kit identity ────────────────────────────────────────────────────────────
    /// <summary>FK to CargoDryKitEntity (same module). No EF navigation — Id-only reference.</summary>
    public long    KitId          { get; private set; }

    /// <summary>Denormalized from kit at attribution time to avoid cross-query joins in reports.</summary>
    public string  SerialNumber   { get; private set; } = default!;

    /// <summary>Denormalized kit code.</summary>
    public string  KitCode        { get; private set; } = default!;

    public string  ProductCode    { get; private set; } = default!;

    /// <summary>Nullable — set when kit was allocated from a specific batch.</summary>
    public string? BatchCode      { get; private set; }

    // ── Commercial context ──────────────────────────────────────────────────────
    /// <summary>Null for DirectSale. Cross-module reference; no EF FK constraint.</summary>
    public long?                   ProviderProfileId      { get; private set; }

    public SalesChannel            SalesChannel           { get; private set; }
    public CargoDryCommercialModel CommercialModel        { get; private set; }

    /// <summary>Set for ConsignmentSellThrough channel. Cross-module reference; no EF FK.</summary>
    public long? ConsignmentAgreementId { get; private set; }

    /// <summary>FK to CargoDryProviderInventoryEntity. Null when no inventory row exists (DirectSale).</summary>
    public long? InventoryId { get; private set; }

    // ── Status ──────────────────────────────────────────────────────────────────
    public CargoDrySalesAttributionStatus Status { get; private set; }

    // ── Financials ──────────────────────────────────────────────────────────────
    /// <summary>Price paid by the end user for the kit. Set during commercial resolution.</summary>
    public decimal? SalePrice       { get; private set; }

    /// <summary>Platform commission percentage (0–100). Null until attributed.</summary>
    public decimal? CommissionRate  { get; private set; }

    /// <summary>Calculated commission amount = SalePrice * CommissionRate / 100.</summary>
    public decimal? CommissionAmount { get; private set; }

    /// <summary>ISO 4217 currency code. Null until attributed.</summary>
    public string?  CurrencyCode    { get; private set; }

    // ── Settlement link ─────────────────────────────────────────────────────────
    /// <summary>
    /// Set when this attribution is included in a CargoDrySellThroughSettlementEntity.
    /// Only applies to ConsignmentSellThrough channel.
    /// </summary>
    public long? SellThroughSettlementId { get; private set; }

    // ── Attribution audit ────────────────────────────────────────────────────────
    /// <summary>When the commercial attribution was resolved (may differ from CreatedAtUtc).</summary>
    public DateTime? AttributedAt       { get; private set; }
    public long?     AttributedByUserId { get; private set; }

    // ── Manual review ───────────────────────────────────────────────────────────
    /// <summary>Free-text note added by admin when resolving CommercialReviewRequired status.</summary>
    public string?   ReviewNote         { get; private set; }
    public long?     ReviewedByUserId   { get; private set; }
    public DateTime? ReviewedAt         { get; private set; }

    // ── Audit ───────────────────────────────────────────────────────────────────
    public DateTime CreatedAtUtc { get; private set; }

    private CargoDrySalesAttributionEntity() { }

    // ── Factory ─────────────────────────────────────────────────────────────────
    public static CargoDrySalesAttributionEntity Create(
        long                            kitId,
        string                          serialNumber,
        string                          kitCode,
        string                          productCode,
        string?                         batchCode,
        SalesChannel                    salesChannel,
        CargoDryCommercialModel         commercialModel,
        CargoDrySalesAttributionStatus  initialStatus,
        DateTime                        nowUtc,
        long?                           providerProfileId      = null,
        long?                           consignmentAgreementId = null,
        long?                           inventoryId            = null,
        decimal?                        salePrice              = null,
        decimal?                        commissionRate         = null,
        decimal?                        commissionAmount       = null,
        string?                         currencyCode           = null,
        long?                           attributedByUserId     = null)
    {
        var entity = new CargoDrySalesAttributionEntity
        {
            KitId                  = kitId,
            SerialNumber           = serialNumber,
            KitCode                = kitCode,
            ProductCode            = productCode,
            BatchCode              = batchCode,
            SalesChannel           = salesChannel,
            CommercialModel        = commercialModel,
            Status                 = initialStatus,
            ProviderProfileId      = providerProfileId,
            ConsignmentAgreementId = consignmentAgreementId,
            InventoryId            = inventoryId,
            SalePrice              = salePrice,
            CommissionRate         = commissionRate,
            CommissionAmount       = commissionAmount,
            CurrencyCode           = currencyCode,
            CreatedAtUtc           = nowUtc,
            IsActive               = true,
        };

        if (initialStatus == CargoDrySalesAttributionStatus.Attributed)
        {
            entity.AttributedAt       = nowUtc;
            entity.AttributedByUserId = attributedByUserId;
        }

        return entity;
    }

    // ── Domain mutation methods ──────────────────────────────────────────────────

    /// <summary>
    /// Links this attribution to a sell-through settlement and marks it as SettlementPending.
    /// </summary>
    public void LinkToSettlement(long settlementId, DateTime nowUtc)
    {
        if (SellThroughSettlementId.HasValue)
            throw new InvalidOperationException(
                $"Attribution {Id} is already linked to settlement {SellThroughSettlementId}.");

        SellThroughSettlementId = settlementId;
        Status                  = CargoDrySalesAttributionStatus.SettlementPending;
    }

    /// <summary>
    /// Marks the attribution as fully Settled (called when the parent settlement is closed).
    /// </summary>
    public void MarkSettled()
    {
        if (Status != CargoDrySalesAttributionStatus.SettlementPending)
            throw new InvalidOperationException(
                $"Cannot mark attribution {Id} as Settled from status {Status}.");

        Status = CargoDrySalesAttributionStatus.Settled;
    }

    /// <summary>
    /// Admin resolves a CommercialReviewRequired attribution manually.
    /// </summary>
    public void ResolveManually(
        decimal  salePrice,
        decimal  commissionRate,
        decimal  commissionAmount,
        string   currencyCode,
        string   reviewNote,
        long     reviewedByUserId,
        DateTime nowUtc)
    {
        if (Status != CargoDrySalesAttributionStatus.CommercialReviewRequired)
            throw new InvalidOperationException(
                $"Attribution {Id} is not in CommercialReviewRequired status (current: {Status}).");

        SalePrice          = salePrice;
        CommissionRate     = commissionRate;
        CommissionAmount   = commissionAmount;
        CurrencyCode       = currencyCode;
        ReviewNote         = reviewNote;
        ReviewedByUserId   = reviewedByUserId;
        ReviewedAt         = nowUtc;
        AttributedAt       = nowUtc;
        AttributedByUserId = reviewedByUserId;
        Status             = CargoDrySalesAttributionStatus.Attributed;
    }

    /// <summary>Cancels the attribution (kit revoked or agreement voided).</summary>
    public void Cancel()
    {
        if (Status is CargoDrySalesAttributionStatus.Settled or CargoDrySalesAttributionStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot cancel attribution {Id} with status {Status}.");

        Status = CargoDrySalesAttributionStatus.Cancelled;
    }
}
