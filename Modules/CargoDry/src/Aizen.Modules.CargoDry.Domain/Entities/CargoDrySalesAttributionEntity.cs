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

    // ── Extended financials (Phase 4A) ──────────────────────────────────────────
    /// <summary>Provider's share of the sale: SalePrice × CommissionRate. Null until financials resolved.</summary>
    public decimal?  ProviderShareAmount      { get; private set; }

    /// <summary>Platform's retained share: SalePrice − ProviderShareAmount. Null until financials resolved.</summary>
    public decimal?  PlatformShareAmount      { get; private set; }

    /// <summary>UTC timestamp when financial amounts were resolved by an admin or automated job.</summary>
    public DateTime? FinancialResolvedAtUtc   { get; private set; }

    /// <summary>Admin user who resolved the financial amounts.</summary>
    public long?     FinancialResolvedByUserId { get; private set; }

    /// <summary>Admin note recorded during financial resolution (e.g., reason for rate override).</summary>
    public string?   ResolutionNote            { get; private set; }

    /// <summary>True when FinancialResolvedAtUtc has a value. Computed — not stored in DB.</summary>
    public bool IsFinanciallyResolved => FinancialResolvedAtUtc.HasValue;

    // ── Settlement link ─────────────────────────────────────────────────────────
    /// <summary>
    /// Set when this attribution is included in a CargoDrySellThroughSettlementEntity.
    /// Only applies to ConsignmentSellThrough channel.
    /// </summary>
    public long? SellThroughSettlementId { get; private set; }

    // ── Phase 5: Commercial rule trace ──────────────────────────────────────────
    /// <summary>
    /// Id of the CommissionRuleEntity that was used to resolve the commission rate.
    /// Null for Admin overrides, agreement-rate resolutions, and product defaults.
    /// Phase 5 (July 2026).
    /// </summary>
    public long?     ResolvedRuleId        { get; private set; }

    /// <summary>
    /// Source tag identifying which tier in the resolution cascade produced the rate.
    /// One of: AdminOverride, CommissionRuleProviderSpecific, CommissionRuleProductChannel,
    ///         ConsignmentAgreement, CargoDryProductDefault, SystemParameterDefault,
    ///         DirectSaleNoProviderShare, Unresolved.
    /// Phase 5 (July 2026).
    /// </summary>
    public string?   ResolvedRuleSource    { get; private set; }

    /// <summary>
    /// Human-readable name of the rule used (copied from CommissionRuleEntity.RuleName at resolution time).
    /// Phase 5 (July 2026).
    /// </summary>
    public string?   ResolvedRuleName      { get; private set; }

    /// <summary>
    /// Commission rate resolved by the rule resolver (0.00–1.00).
    /// Stored separately from CommissionRate for comparison / audit.
    /// Phase 5 (July 2026).
    /// </summary>
    public decimal?  ResolvedRate          { get; private set; }

    /// <summary>UTC timestamp when the rule was resolved.</summary>
    public DateTime? RateResolvedAtUtc     { get; private set; }

    /// <summary>Admin user who triggered the financial resolution that ran the resolver.</summary>
    public long?     RateResolvedByUserId  { get; private set; }

    /// <summary>
    /// Admin note specific to the rule resolution (separate from ResolutionNote which covers financials).
    /// Phase 5 (July 2026).
    /// </summary>
    public string?   RuleResolutionNote    { get; private set; }

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

    /// <summary>
    /// Resolves the financial amounts for this attribution.
    /// Can be called on any status except Settled or Cancelled.
    /// CommissionRate is the provider's share rate (0.00–1.00):
    ///   ProviderShareAmount = SalePrice × CommissionRate
    ///   PlatformShareAmount = SalePrice − ProviderShareAmount
    /// Phase 4A (July 2026): Financial Resolution.
    /// </summary>
    public void ResolveFinancials(
        decimal  salePrice,
        decimal  commissionRate,
        string   currencyCode,
        DateTime resolvedAtUtc,
        long     resolvedByUserId,
        string?  resolutionNote = null)
    {
        if (Status is CargoDrySalesAttributionStatus.Settled
                   or CargoDrySalesAttributionStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot resolve financials on attribution {Id} with status {Status}.");

        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice), "SalePrice must be non-negative.");

        if (commissionRate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(commissionRate),
                "CommissionRate must be between 0.00 and 1.00.");

        var providerShare  = Math.Round(salePrice * commissionRate,      4, MidpointRounding.AwayFromZero);
        var platformShare  = Math.Round(salePrice - providerShare,       4, MidpointRounding.AwayFromZero);
        var commissionAmt  = providerShare; // CommissionAmount = provider's earnings (their commission)

        SalePrice               = salePrice;
        CommissionRate          = commissionRate;
        CommissionAmount        = commissionAmt;
        CurrencyCode            = currencyCode;
        ProviderShareAmount     = providerShare;
        PlatformShareAmount     = platformShare;
        FinancialResolvedAtUtc  = resolvedAtUtc;
        FinancialResolvedByUserId = resolvedByUserId;
        ResolutionNote          = resolutionNote;
    }

    /// <summary>
    /// Records the commercial rule resolution trace on this attribution.
    /// Called by ResolveCargoDrySalesAttributionFinancialsCommandHandler after the resolver runs.
    /// Can be called on any status except Settled or Cancelled.
    /// Phase 5 (July 2026).
    /// </summary>
    public void RecordRuleTrace(
        string   ruleSource,
        decimal  resolvedRate,
        DateTime resolvedAtUtc,
        long     resolvedByUserId,
        long?    ruleId            = null,
        string?  ruleName          = null,
        string?  ruleResolutionNote = null)
    {
        if (Status is CargoDrySalesAttributionStatus.Settled
                   or CargoDrySalesAttributionStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot record rule trace on attribution {Id} with status {Status}.");

        ResolvedRuleId       = ruleId;
        ResolvedRuleSource   = ruleSource;
        ResolvedRuleName     = ruleName;
        ResolvedRate         = resolvedRate;
        RateResolvedAtUtc    = resolvedAtUtc;
        RateResolvedByUserId = resolvedByUserId;
        RuleResolutionNote   = ruleResolutionNote;
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
