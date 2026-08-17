using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;

/// <summary>
/// BE-S5a (§20.9) — a versioned, scoped commercial term for <b>Product / Consumable</b> part lines: the supplier/dealer cost +
/// margin, the funded-discount split (supplier / provider / platform), the <see cref="MinimumProviderReceivable"/> floor and the
/// <see cref="MaximumDiscountableAmount"/> cap. Mirrors the Payment versioned-scoped resolver pattern (CommissionRule / PlatformFeeRule
/// / ProviderPlanPrice / CustomerDiscountRule): private setters, validating factory, append-a-new-version (never mutate an active row),
/// specificity + priority + effective-date resolution with a fail-loud conflict.
///
/// <para><b>Cost confidentiality (headline, §20.9):</b> <see cref="SupplierListPrice"/> and <see cref="ProviderDealerMargin"/> are the
/// provider's confidential cost/margin. They live here, Payment-internal, and <b>never</b> cross the module boundary — no SR DTO, no
/// BFF response, no log line, no preview. Only the derived, cost-free <c>PartLineAllowanceDto</c> (max allowed discount + funded split +
/// min-receivable) leaves Payment. This entity is <b>never referenced by ServiceRequest</b>.</para>
///
/// <para><b>Descriptive/config (this phase):</b> S5 <b>defines + resolves</b> terms. It does <b>not</b> apply a part discount or move
/// any offer/acceptance total or the 8-equality — that is S9. No economics path reads this entity in S5.</para>
/// </summary>
[DocumentationInfo("Part commercial term entity",
    "Versioned, scoped commercial term for Product/Consumable lines (dealer cost + margin + funded split + min-receivable + " +
    "max-discountable). Cost fields are confidential and never leave the Payment module. Defines/resolves only — applies nothing (S9).")]
[NoMessagebusSync] // domain-authored cost-confidential S5 commercial term — never generically writable
public sealed class PartCommercialTermEntity : AizenEntityWithAudit
{
    // ── Scope (nullable → specificity, like the other rules): product > provider > brand > category > global ──
    public string? Brand             { get; private set; }
    public string? ProductCode       { get; private set; }
    public long?   ProviderProfileId { get; private set; }
    public string? CategoryCode      { get; private set; }
    public string  CurrencyCode      { get; private set; } = "TRY";

    // ── Confidential cost / margin (NEVER leaves Payment) ──
    public decimal SupplierListPrice    { get; private set; }
    public decimal ProviderDealerMargin { get; private set; }

    // ── Derived caps + funded split (the cost-free projection is built from these) ──
    public decimal MaxCustomerDiscount       { get; private set; }
    public decimal SupplierFundedAmount      { get; private set; }
    public decimal ProviderFundedAmount      { get; private set; }
    public decimal PlatformFundedAmount      { get; private set; }
    public decimal MinimumProviderReceivable { get; private set; }
    public decimal MaximumDiscountableAmount { get; private set; }

    // ── Versioning + lifecycle / admin ──
    public int                    Version       { get; private set; }
    public CommissionRulePriority Priority      { get; private set; }
    public DateTime               EffectiveFrom { get; private set; }
    public DateTime?              EffectiveTo   { get; private set; }
    public CommissionRuleStatus   Status        { get; private set; }
    public string?                TermCode      { get; private set; }
    public string?                TermName      { get; private set; }
    public string?                Notes         { get; private set; }

    private PartCommercialTermEntity() { }

    public static PartCommercialTermEntity Create(
        string? brand, string? productCode, long? providerProfileId, string? categoryCode, string currencyCode,
        decimal supplierListPrice, decimal providerDealerMargin,
        decimal maxCustomerDiscount, decimal supplierFundedAmount, decimal providerFundedAmount, decimal platformFundedAmount,
        decimal minimumProviderReceivable, decimal maximumDiscountableAmount,
        int version, CommissionRulePriority priority,
        DateTime effectiveFrom, DateTime? effectiveTo,
        string? termCode, string? termName = null, string? notes = null)
    {
        Validate(supplierListPrice, providerDealerMargin, maxCustomerDiscount, supplierFundedAmount, providerFundedAmount,
            platformFundedAmount, minimumProviderReceivable, maximumDiscountableAmount, effectiveFrom, effectiveTo);

        return new PartCommercialTermEntity
        {
            Brand                     = Normalize(brand),
            ProductCode               = Normalize(productCode),
            ProviderProfileId         = providerProfileId,
            CategoryCode              = Normalize(categoryCode),
            CurrencyCode              = currencyCode.ToUpperInvariant(),
            SupplierListPrice         = supplierListPrice,
            ProviderDealerMargin      = providerDealerMargin,
            MaxCustomerDiscount       = maxCustomerDiscount,
            SupplierFundedAmount      = supplierFundedAmount,
            ProviderFundedAmount      = providerFundedAmount,
            PlatformFundedAmount      = platformFundedAmount,
            MinimumProviderReceivable = minimumProviderReceivable,
            MaximumDiscountableAmount = maximumDiscountableAmount,
            Version                   = version < 1 ? 1 : version,
            Priority                  = priority,
            EffectiveFrom             = effectiveFrom,
            EffectiveTo               = effectiveTo,
            TermCode                  = termCode,
            TermName                  = termName,
            Notes                     = notes,
            IsActive                  = true,
            Status                    = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public void Update(
        decimal supplierListPrice, decimal providerDealerMargin,
        decimal maxCustomerDiscount, decimal supplierFundedAmount, decimal providerFundedAmount, decimal platformFundedAmount,
        decimal minimumProviderReceivable, decimal maximumDiscountableAmount,
        CommissionRulePriority priority, DateTime effectiveFrom, DateTime? effectiveTo, string? termName, string? notes)
    {
        Validate(supplierListPrice, providerDealerMargin, maxCustomerDiscount, supplierFundedAmount, providerFundedAmount,
            platformFundedAmount, minimumProviderReceivable, maximumDiscountableAmount, effectiveFrom, effectiveTo);

        SupplierListPrice         = supplierListPrice;
        ProviderDealerMargin      = providerDealerMargin;
        MaxCustomerDiscount       = maxCustomerDiscount;
        SupplierFundedAmount      = supplierFundedAmount;
        ProviderFundedAmount      = providerFundedAmount;
        PlatformFundedAmount      = platformFundedAmount;
        MinimumProviderReceivable = minimumProviderReceivable;
        MaximumDiscountableAmount = maximumDiscountableAmount;
        Priority                  = priority;
        EffectiveFrom             = effectiveFrom;
        EffectiveTo               = effectiveTo;
        TermName                  = termName;
        Notes                     = notes;
        Status                    = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate() { IsActive = false; Status = CommissionRuleStatus.Inactive; }
    public void Reactivate() { IsActive = true;  Status = DeriveStatus(EffectiveFrom, EffectiveTo); }
    public void SetTermCode(string termCode) => TermCode = termCode;
    public void SetVersion(int version)      => Version = version < 1 ? 1 : version;

    /// <summary>Half-open membership: <c>EffectiveFrom ≤ atUtc &lt; EffectiveTo</c> (the upper bound belongs to the next version).</summary>
    public bool CoversInstant(DateTime atUtc)
        => IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    private static void Validate(
        decimal supplierListPrice, decimal providerDealerMargin,
        decimal maxCustomerDiscount, decimal supplierFundedAmount, decimal providerFundedAmount, decimal platformFundedAmount,
        decimal minimumProviderReceivable, decimal maximumDiscountableAmount, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        AizenBusinessException Invalid(string m) => new((int)PaymentErrorCode.PartCommercialTermInvalid, m);

        // All money ≥ 0.
        if (supplierListPrice         < 0m) throw Invalid("SupplierListPrice must be ≥ 0.");
        if (providerDealerMargin      < 0m) throw Invalid("ProviderDealerMargin must be ≥ 0.");
        if (maxCustomerDiscount       < 0m) throw Invalid("MaxCustomerDiscount must be ≥ 0.");
        if (supplierFundedAmount      < 0m) throw Invalid("SupplierFundedAmount must be ≥ 0.");
        if (providerFundedAmount      < 0m) throw Invalid("ProviderFundedAmount must be ≥ 0.");
        if (platformFundedAmount      < 0m) throw Invalid("PlatformFundedAmount must be ≥ 0.");
        if (minimumProviderReceivable < 0m) throw Invalid("MinimumProviderReceivable must be ≥ 0.");
        if (maximumDiscountableAmount < 0m) throw Invalid("MaximumDiscountableAmount must be ≥ 0.");

        // Σ(funded) ≤ maxDiscountable — the funded split can never exceed the discountable cap.
        if (supplierFundedAmount + providerFundedAmount + platformFundedAmount > maximumDiscountableAmount)
            throw Invalid("Σ(SupplierFunded + ProviderFunded + PlatformFunded) must be ≤ MaximumDiscountableAmount.");

        // maxCustomerDiscount ≤ maxDiscountable.
        if (maxCustomerDiscount > maximumDiscountableAmount)
            throw Invalid("MaxCustomerDiscount must be ≤ MaximumDiscountableAmount.");

        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw Invalid("EffectiveTo must be after EffectiveFrom.");
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
}
