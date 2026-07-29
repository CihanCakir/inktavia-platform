using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;

/// <summary>
/// Authoritative customer-discount model (§19.6): plan / category / campaign scope, funding source, bounds. Replaces
/// ad-hoc plan discounts — the legacy <c>ParticipantPlan.ServiceDiscountRate</c> is reconciled by seeding it as a
/// PlatformFunded, plan-scoped rule and kept for display only. Resolution mirrors BE-P2 (specificity + Priority +
/// fail-loud conflict). <b>Binding: no rule without a funding source.</b>
/// </summary>
[DocumentationInfo("Customer discount rule entity",
    "Authoritative customer discount (plan/category/global, Percent/Fixed, Min/Max, funding). No rule may exist " +
    "without a funding source; ProviderFunded requires provider consent — never silently platform-funded.")]
public sealed class CustomerDiscountRuleEntity : AizenEntityWithAudit
{
    // ── Targeting ────────────────────────────────────────────────────────────────
    public long?   CustomerPlanId { get; private set; }   // participant plan; null = any
    public string? CategoryCode   { get; private set; }   // null = any category
    public string  CurrencyCode   { get; private set; } = "TRY";

    // ── Discount definition ──────────────────────────────────────────────────────
    public CustomerDiscountType DiscountType        { get; private set; }
    public decimal?             DiscountRate        { get; private set; }   // Percent → fraction, e.g. 0.05
    public decimal?             FixedDiscountAmount { get; private set; }   // Fixed
    public decimal?             MinimumPurchaseAmount { get; private set; } // rule inert below this base
    public decimal?             MaximumDiscountAmount { get; private set; } // clamp

    // ── Funding (§19.6) ──────────────────────────────────────────────────────────
    public CustomerDiscountFundingMode FundingMode         { get; private set; }
    public decimal?                    PlatformFundingRate { get; private set; }   // Shared → sums to 1.0 with provider
    public decimal?                    ProviderFundingRate { get; private set; }
    /// <summary>ProviderFunded/Shared: the provider portion applies only with prior explicit consent (default true).</summary>
    public bool                        RequiresProviderConsent { get; private set; }

    // ── Lifecycle / admin ────────────────────────────────────────────────────────
    public CommissionRulePriority Priority      { get; private set; }
    public DateTime               EffectiveFrom { get; private set; }
    public DateTime?              EffectiveTo   { get; private set; }
    public CommissionRuleStatus   Status        { get; private set; }
    public string?                RuleCode      { get; private set; }
    public string?                RuleName      { get; private set; }
    public string?                Notes         { get; private set; }

    private CustomerDiscountRuleEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static CustomerDiscountRuleEntity Create(
        long?   customerPlanId,
        string? categoryCode,
        string  currencyCode,
        CustomerDiscountType discountType,
        decimal? discountRate,
        decimal? fixedDiscountAmount,
        decimal? minimumPurchaseAmount,
        decimal? maximumDiscountAmount,
        CustomerDiscountFundingMode fundingMode,
        decimal? platformFundingRate,
        decimal? providerFundingRate,
        bool     requiresProviderConsent,
        CommissionRulePriority priority,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string?  ruleCode,
        string?  ruleName = null,
        string?  notes = null)
    {
        Validate(discountType, discountRate, fixedDiscountAmount, fundingMode,
            platformFundingRate, providerFundingRate, minimumPurchaseAmount, maximumDiscountAmount, effectiveFrom, effectiveTo);

        return new CustomerDiscountRuleEntity
        {
            CustomerPlanId          = customerPlanId,
            CategoryCode            = categoryCode?.ToUpperInvariant(),
            CurrencyCode            = currencyCode.ToUpperInvariant(),
            DiscountType            = discountType,
            DiscountRate            = discountRate,
            FixedDiscountAmount     = fixedDiscountAmount,
            MinimumPurchaseAmount   = minimumPurchaseAmount,
            MaximumDiscountAmount   = maximumDiscountAmount,
            FundingMode             = fundingMode,
            PlatformFundingRate     = platformFundingRate,
            ProviderFundingRate     = providerFundingRate,
            RequiresProviderConsent = requiresProviderConsent,
            Priority                = priority,
            EffectiveFrom           = effectiveFrom,
            EffectiveTo             = effectiveTo,
            RuleCode                = ruleCode,
            RuleName                = ruleName,
            Notes                   = notes,
            IsActive                = true,
            Status                  = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public void Update(
        CustomerDiscountType discountType, decimal? discountRate, decimal? fixedDiscountAmount,
        decimal? minimumPurchaseAmount, decimal? maximumDiscountAmount,
        CustomerDiscountFundingMode fundingMode, decimal? platformFundingRate, decimal? providerFundingRate,
        bool requiresProviderConsent, CommissionRulePriority priority,
        DateTime effectiveFrom, DateTime? effectiveTo, string? ruleName, string? notes)
    {
        Validate(discountType, discountRate, fixedDiscountAmount, fundingMode,
            platformFundingRate, providerFundingRate, minimumPurchaseAmount, maximumDiscountAmount, effectiveFrom, effectiveTo);

        DiscountType            = discountType;
        DiscountRate            = discountRate;
        FixedDiscountAmount     = fixedDiscountAmount;
        MinimumPurchaseAmount   = minimumPurchaseAmount;
        MaximumDiscountAmount   = maximumDiscountAmount;
        FundingMode             = fundingMode;
        PlatformFundingRate     = platformFundingRate;
        ProviderFundingRate     = providerFundingRate;
        RequiresProviderConsent = requiresProviderConsent;
        Priority                = priority;
        EffectiveFrom           = effectiveFrom;
        EffectiveTo             = effectiveTo;
        RuleName                = ruleName;
        Notes                   = notes;
        Status                  = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    public void SetRuleCode(string ruleCode) => RuleCode = ruleCode;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    // ── Requested-discount computation (§19.6 step 3) — pure ────────────────────

    /// <summary>
    /// Computes the requested (gross) discount for a service base: Percent → Round(base × Rate); Fixed → FixedAmount.
    /// Inert (0) when the base is below <see cref="MinimumPurchaseAmount"/>; clamped to <see cref="MaximumDiscountAmount"/>.
    /// Never exceeds the base.
    /// </summary>
    public decimal ComputeRequestedDiscount(decimal serviceBaseAmount)
    {
        if (MinimumPurchaseAmount is { } min && serviceBaseAmount < min)
            return 0m;

        var raw = DiscountType == CustomerDiscountType.Percent
            ? MoneyMath.Round(serviceBaseAmount * (DiscountRate ?? 0m))
            : MoneyMath.Round(FixedDiscountAmount ?? 0m);

        if (MaximumDiscountAmount is { } max && raw > max)
            raw = max;

        if (raw > serviceBaseAmount) raw = serviceBaseAmount;
        return raw < 0m ? 0m : raw;
    }

    // ── Validation / helpers ────────────────────────────────────────────────────

    private static void Validate(
        CustomerDiscountType discountType, decimal? discountRate, decimal? fixedDiscountAmount,
        CustomerDiscountFundingMode fundingMode, decimal? platformFundingRate, decimal? providerFundingRate,
        decimal? minimumPurchaseAmount, decimal? maximumDiscountAmount, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        AizenBusinessException Invalid(string m) => new((int)PaymentErrorCode.CustomerDiscountRuleInvalid, m);

        // ── Discount coherence ──
        if (discountType == CustomerDiscountType.Percent && discountRate is null or <= 0m)
            throw Invalid("Percent discount requires a positive DiscountRate.");
        if (discountType == CustomerDiscountType.Fixed && fixedDiscountAmount is null or <= 0m)
            throw Invalid("Fixed discount requires a positive FixedDiscountAmount.");

        if (minimumPurchaseAmount is < 0m) throw Invalid("MinimumPurchaseAmount must be ≥ 0.");
        if (maximumDiscountAmount is < 0m) throw Invalid("MaximumDiscountAmount must be ≥ 0.");

        // ── Funding source is mandatory (binding, §19.6) ──
        if (!Enum.IsDefined(typeof(CustomerDiscountFundingMode), fundingMode))
            throw Invalid("A funding source (FundingMode) is required — a discount without funding cannot exist.");

        if (fundingMode == CustomerDiscountFundingMode.Shared)
        {
            if (platformFundingRate is null || providerFundingRate is null)
                throw Invalid("Shared funding requires both PlatformFundingRate and ProviderFundingRate.");
            if (platformFundingRate < 0m || providerFundingRate < 0m)
                throw Invalid("Shared funding rates must be ≥ 0.");
            if (platformFundingRate.Value + providerFundingRate.Value != 1.0m)
                throw Invalid("Shared funding rates must sum to 1.0 (100%).");
        }

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
}
