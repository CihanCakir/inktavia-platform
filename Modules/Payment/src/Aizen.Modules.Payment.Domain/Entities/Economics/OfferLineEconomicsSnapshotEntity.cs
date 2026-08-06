using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// Immutable, insert-only per-line economics snapshot (§20.15) — a child of the single aggregate
/// <see cref="PaymentEconomicsSnapshotEntity"/> (FK, OnDelete Restrict). Written once by
/// <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/>; never mutated (private setters, no methods).
/// The aggregate's 8 equalities are the exact sums of these rows. <c>ItemType</c>/<c>PricingMethod</c> are stored as the
/// raw ServiceRequest enum int values (Payment holds them opaquely — no cross-module enum dependency).
/// </summary>
[DocumentationInfo("Offer line economics snapshot entity",
    "Immutable per-line child of PaymentEconomicsSnapshot. Line gross/discount/commission/net/vat/total; the aggregate " +
    "totals are the sum of these rows (§20.15).")]
public sealed class OfferLineEconomicsSnapshotEntity : AizenEntityWithAudit
{
    public long                     EconomicsSnapshotId          { get; private set; }
    public string                   LineRef                      { get; private set; } = default!;
    public int                      ItemType                     { get; private set; }   // raw SR ServiceRequestOfferItemType value
    public int                      PricingMethod                { get; private set; }   // raw SR PricingMethod value
    public decimal                  LineGrossBeforeDiscount      { get; private set; }
    public decimal                  CustomerDiscountAmount       { get; private set; }
    public decimal                  ProviderFundedDiscountAmount { get; private set; }
    public decimal                  PlatformFundedDiscountAmount { get; private set; }
    public LineCommissionEligibility CommissionEligibility       { get; private set; }
    public decimal                  CommissionBaseAmount         { get; private set; }
    public decimal                  CommissionRate               { get; private set; }
    public decimal                  CommissionAmount             { get; private set; }
    public decimal                  ProviderNetAmount            { get; private set; }
    public decimal                  LineVatAmount                { get; private set; }
    public decimal                  LineTotalAmount              { get; private set; }
    public string                   CurrencyCode                 { get; private set; } = "TRY";
    public int                      SortOrder                    { get; private set; }

    // ── BE-S9 line-level profit protection (§20.12) — descriptive, insert-only; NOT in the money math or the 8 equalities.
    //    Recorded on approval: the provider-minimum-receivable floor applied to the line (part: S5; else policy), the line's
    //    computed platform contribution, and whether the line cleared its own floor. Line-level gating happens BEFORE the
    //    transaction gates; on failure NO snapshot is written (the LineProfitProtectionEvaluationLog is the record). ──
    public decimal                  LineMinProviderReceivableApplied { get; private set; }
    public decimal                  LinePlatformContribution         { get; private set; }
    public bool                     LineProfitProtectionPassed       { get; private set; } = true;

    // ── S3 frozen FX metadata (§20.7) — null for a settlement-native line. Self-contained (source ccy + price + applied
    //    rate + rate date + resolved TRY unit price); descriptive, NOT part of the money math or the 8 equalities. The
    //    line amounts above are already TRY. After acceptance this is never re-resolved — a later rate change cannot move
    //    the accepted total. ──
    public string?   FxSourceCurrencyCode     { get; private set; }
    public string?   FxSettlementCurrencyCode { get; private set; }
    public decimal?  FxSourceUnitPrice        { get; private set; }
    public decimal?  FxAppliedRate            { get; private set; }
    public DateTime? FxRateDate               { get; private set; }
    public decimal?  FxResolvedUnitPrice      { get; private set; }

    // ── S2d pricing attribute snapshots (§20.6/§20.15) — immutable, insert-only; descriptive, NOT in the money math ──
    private readonly List<OfferLineAttributeSnapshotEntity> _attributeSnapshots = new();
    public IReadOnlyCollection<OfferLineAttributeSnapshotEntity> AttributeSnapshots => _attributeSnapshots.AsReadOnly();

    private OfferLineEconomicsSnapshotEntity() { }

    internal static OfferLineEconomicsSnapshotEntity Create(
        string lineRef, int itemType, int pricingMethod,
        decimal lineGrossBeforeDiscount, decimal customerDiscount, decimal providerFundedDiscount, decimal platformFundedDiscount,
        LineCommissionEligibility commissionEligibility, decimal commissionBase, decimal commissionRate, decimal commissionAmount,
        decimal providerNet, decimal lineVat, decimal lineTotal, string currencyCode, int sortOrder,
        IReadOnlyList<OfferLineAttributeSnapshotEntity>? attributeSnapshots = null,
        LineFxSnapshotInput? fx = null,
        // ── BE-S9 descriptive line-level protection record (defaults preserve the pre-S9 line snapshot) ──
        decimal lineMinProviderReceivableApplied = 0m,
        decimal linePlatformContribution = 0m,
        bool lineProfitProtectionPassed = true)
    {
        var entity = new OfferLineEconomicsSnapshotEntity
        {
            LineRef                      = lineRef,
            ItemType                     = itemType,
            PricingMethod                = pricingMethod,
            LineGrossBeforeDiscount      = lineGrossBeforeDiscount,
            CustomerDiscountAmount       = customerDiscount,
            ProviderFundedDiscountAmount = providerFundedDiscount,
            PlatformFundedDiscountAmount = platformFundedDiscount,
            CommissionEligibility        = commissionEligibility,
            CommissionBaseAmount         = commissionBase,
            CommissionRate               = commissionRate,
            CommissionAmount             = commissionAmount,
            ProviderNetAmount            = providerNet,
            LineVatAmount                = lineVat,
            LineTotalAmount              = lineTotal,
            CurrencyCode                 = currencyCode.ToUpperInvariant(),
            SortOrder                    = sortOrder,
            LineMinProviderReceivableApplied = lineMinProviderReceivableApplied,
            LinePlatformContribution         = linePlatformContribution,
            LineProfitProtectionPassed       = lineProfitProtectionPassed,
            IsActive                     = true,
        };
        if (attributeSnapshots is { Count: > 0 })
            entity._attributeSnapshots.AddRange(attributeSnapshots);
        entity.ApplyFx(fx);
        return entity;
    }

    /// <summary>
    /// S3 — folds the frozen FX record onto the line (tamper → throw). Verifies the record is self-consistent: a non-settlement
    /// source currency, a positive rate, and <c>ResolvedUnitPrice == MoneyMath.Round(SourceUnitPrice × AppliedRate)</c> using
    /// the exact S1 money-rounding convention — so a persisted FX row can never silently disagree with its own numbers.
    /// </summary>
    private void ApplyFx(LineFxSnapshotInput? fx)
    {
        if (fx is null) return;

        var source     = (fx.SourceCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        var settlement = (fx.SettlementCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();

        if (source.Length == 0 || settlement.Length == 0)
            throw new PaymentEconomicsInvariantException("OfferLine FX snapshot requires source and settlement currency codes.");
        if (source == settlement)
            throw new PaymentEconomicsInvariantException("OfferLine FX snapshot is only for a non-settlement source currency.");
        if (fx.AppliedRate <= 0m)
            throw new PaymentEconomicsInvariantException($"OfferLine FX snapshot requires a positive rate for {source}->{settlement}.");
        if (fx.SourceUnitPrice < 0m)
            throw new PaymentEconomicsInvariantException("OfferLine FX snapshot SourceUnitPrice must be >= 0.");
        if (fx.ResolvedUnitPrice != MoneyMath.Round(fx.SourceUnitPrice * fx.AppliedRate))
            throw new PaymentEconomicsInvariantException(
                "OfferLine FX snapshot ResolvedUnitPrice == round(SourceUnitPrice * AppliedRate)");

        FxSourceCurrencyCode     = source;
        FxSettlementCurrencyCode = settlement;
        FxSourceUnitPrice        = fx.SourceUnitPrice;
        FxAppliedRate            = fx.AppliedRate;
        FxRateDate               = fx.RateDate.Kind == DateTimeKind.Utc ? fx.RateDate : DateTime.SpecifyKind(fx.RateDate, DateTimeKind.Utc);
        FxResolvedUnitPrice      = fx.ResolvedUnitPrice;
    }
}
