using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

/// <summary>
/// A provider commission-benefit rule (§19.4/§19.5): a <b>discount on the commission RATE</b> — expressed as
/// <see cref="AdjustmentPercentagePoints"/> (a rate fraction, negative = discount; e.g. −0.0100 = −1pp). This is
/// strictly separate from the premium <c>OFFER_BOOST_7D</c> (P11), which only affects visibility and NEVER the commission
/// rate — enforced by construction (P7 code never reads premium products). Applied on top of the BE-P2 base rate as a
/// second stage; versioned + effective-dated like BE-P2..P6.
/// </summary>
[DocumentationInfo("Provider commission benefit rule entity",
    "Commission-rate discount (AdjustmentPercentagePoints, negative = discount) with MinimumCommissionRate floor, " +
    "MaximumDiscountAmount, MaximumEligibleGMV, UsageLimit, Stackable/Exclusive. Decoupled from premium boost.")]
public sealed class ProviderCommissionBenefitRuleEntity : AizenEntityWithAudit
{
    public string?  RuleCode                { get; private set; }
    public string?  RuleName                { get; private set; }

    // ── Scope (null = broad/campaign) ───────────────────────────────────────────
    public long?    ProviderProfileId       { get; private set; }
    public long?    ProviderPlanId          { get; private set; }
    /// <summary>Comma-separated upper-case category codes; null/empty = all categories.</summary>
    public string?  ApplicableCategoryCodesCsv { get; private set; }

    // ── Benefit definition ──────────────────────────────────────────────────────
    /// <summary>Rate adjustment as a fraction; negative = discount (e.g. −0.0100 = −1pp). Surcharge (&gt;0) is rejected.</summary>
    public decimal  AdjustmentPercentagePoints { get; private set; }
    /// <summary>Rule-level floor: the effective rate cannot go below this (control 4). ∈ [0,1].</summary>
    public decimal  MinimumCommissionRate   { get; private set; }
    public decimal? MaximumDiscountAmount   { get; private set; }   // per-transaction monetary cap (control 6)
    public decimal? MaximumEligibleGMV      { get; private set; }   // benefit only within this GMV (control 5)
    public long?    UsageLimit              { get; private set; }   // max applications
    public bool     Stackable               { get; private set; }
    public bool     Exclusive               { get; private set; }

    // ── Lifecycle / admin ────────────────────────────────────────────────────────
    public CommissionRulePriority Priority      { get; private set; }
    public DateTime               EffectiveFrom { get; private set; }
    public DateTime?              EffectiveTo   { get; private set; }
    public string                 CurrencyCode  { get; private set; } = "TRY";
    public CommissionRuleStatus   Status        { get; private set; }
    public string?                Notes         { get; private set; }

    private ProviderCommissionBenefitRuleEntity() { }

    public static ProviderCommissionBenefitRuleEntity Create(
        string? ruleCode, string? ruleName,
        long? providerProfileId, long? providerPlanId, IEnumerable<string>? applicableCategoryCodes,
        decimal adjustmentPercentagePoints, decimal minimumCommissionRate,
        decimal? maximumDiscountAmount, decimal? maximumEligibleGmv, long? usageLimit,
        bool stackable, bool exclusive,
        CommissionRulePriority priority, DateTime effectiveFrom, DateTime? effectiveTo,
        string currencyCode, string? notes = null)
    {
        Validate(adjustmentPercentagePoints, minimumCommissionRate, maximumDiscountAmount, maximumEligibleGmv,
            usageLimit, stackable, exclusive, effectiveFrom, effectiveTo);

        return new ProviderCommissionBenefitRuleEntity
        {
            RuleCode                   = ruleCode,
            RuleName                   = ruleName,
            ProviderProfileId          = providerProfileId,
            ProviderPlanId             = providerPlanId,
            ApplicableCategoryCodesCsv = NormalizeCsv(applicableCategoryCodes),
            AdjustmentPercentagePoints = adjustmentPercentagePoints,
            MinimumCommissionRate      = minimumCommissionRate,
            MaximumDiscountAmount      = maximumDiscountAmount,
            MaximumEligibleGMV         = maximumEligibleGmv,
            UsageLimit                 = usageLimit,
            Stackable                  = stackable,
            Exclusive                  = exclusive,
            Priority                   = priority,
            EffectiveFrom              = effectiveFrom,
            EffectiveTo                = effectiveTo,
            CurrencyCode               = currencyCode.ToUpperInvariant(),
            Notes                      = notes,
            IsActive                   = true,
            Status                     = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public void Update(
        string? ruleName, IEnumerable<string>? applicableCategoryCodes,
        decimal adjustmentPercentagePoints, decimal minimumCommissionRate,
        decimal? maximumDiscountAmount, decimal? maximumEligibleGmv, long? usageLimit,
        bool stackable, bool exclusive, CommissionRulePriority priority,
        DateTime effectiveFrom, DateTime? effectiveTo, string? notes)
    {
        Validate(adjustmentPercentagePoints, minimumCommissionRate, maximumDiscountAmount, maximumEligibleGmv,
            usageLimit, stackable, exclusive, effectiveFrom, effectiveTo);

        RuleName                   = ruleName;
        ApplicableCategoryCodesCsv = NormalizeCsv(applicableCategoryCodes);
        AdjustmentPercentagePoints = adjustmentPercentagePoints;
        MinimumCommissionRate      = minimumCommissionRate;
        MaximumDiscountAmount      = maximumDiscountAmount;
        MaximumEligibleGMV         = maximumEligibleGmv;
        UsageLimit                 = usageLimit;
        Stackable                  = stackable;
        Exclusive                  = exclusive;
        Priority                   = priority;
        EffectiveFrom              = effectiveFrom;
        EffectiveTo                = effectiveTo;
        Notes                      = notes;
        Status                     = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    public void SetRuleCode(string ruleCode) => RuleCode = ruleCode;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    /// <summary>Scope match against a resolve context (provider / plan / category set membership).</summary>
    public bool MatchesScope(long providerProfileId, long? providerPlanId, string? categoryCode)
    {
        if (ProviderProfileId.HasValue && ProviderProfileId != providerProfileId) return false;
        if (ProviderPlanId.HasValue && ProviderPlanId != providerPlanId) return false;

        var codes = ApplicableCategoryCodes;
        if (codes.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(categoryCode)) return false;
            if (!codes.Contains(categoryCode.ToUpperInvariant())) return false;
        }
        return true;
    }

    public IReadOnlyCollection<string> ApplicableCategoryCodes =>
        string.IsNullOrWhiteSpace(ApplicableCategoryCodesCsv)
            ? Array.Empty<string>()
            : ApplicableCategoryCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // ── Validation / helpers ────────────────────────────────────────────────────

    private static void Validate(
        decimal adjustment, decimal minRate, decimal? maxDiscount, decimal? maxGmv, long? usageLimit,
        bool stackable, bool exclusive, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        AizenBusinessException Invalid(string m) => new((int)PaymentErrorCode.ProviderCommissionBenefitRuleInvalid, m);

        if (adjustment > 0m)
            throw Invalid("AdjustmentPercentagePoints must be ≤ 0 (a benefit is a discount, not a surcharge).");
        if (minRate < 0m || minRate > 1m)
            throw Invalid("MinimumCommissionRate must be within [0,1].");
        if (maxDiscount is < 0m) throw Invalid("MaximumDiscountAmount must be ≥ 0.");
        if (maxGmv is < 0m)      throw Invalid("MaximumEligibleGMV must be ≥ 0.");
        if (usageLimit is < 0)   throw Invalid("UsageLimit must be ≥ 0.");
        if (exclusive && stackable)
            throw Invalid("A rule cannot be both Exclusive and Stackable.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw Invalid("EffectiveTo must be after EffectiveFrom.");
    }

    private static string? NormalizeCsv(IEnumerable<string>? codes)
    {
        if (codes is null) return null;
        var list = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList();
        return list.Count == 0 ? null : string.Join(',', list);
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
