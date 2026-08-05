using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

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

    // ── S2d pricing attribute snapshots (§20.6/§20.15) — immutable, insert-only; descriptive, NOT in the money math ──
    private readonly List<OfferLineAttributeSnapshotEntity> _attributeSnapshots = new();
    public IReadOnlyCollection<OfferLineAttributeSnapshotEntity> AttributeSnapshots => _attributeSnapshots.AsReadOnly();

    private OfferLineEconomicsSnapshotEntity() { }

    internal static OfferLineEconomicsSnapshotEntity Create(
        string lineRef, int itemType, int pricingMethod,
        decimal lineGrossBeforeDiscount, decimal customerDiscount, decimal providerFundedDiscount, decimal platformFundedDiscount,
        LineCommissionEligibility commissionEligibility, decimal commissionBase, decimal commissionRate, decimal commissionAmount,
        decimal providerNet, decimal lineVat, decimal lineTotal, string currencyCode, int sortOrder,
        IReadOnlyList<OfferLineAttributeSnapshotEntity>? attributeSnapshots = null)
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
            IsActive                     = true,
        };
        if (attributeSnapshots is { Count: > 0 })
            entity._attributeSnapshots.AddRange(attributeSnapshots);
        return entity;
    }
}
