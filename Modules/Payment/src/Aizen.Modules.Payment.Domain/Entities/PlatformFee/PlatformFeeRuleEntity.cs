using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.PlatformFee;

/// <summary>
/// Data-driven platform fee rule (BE-P3, §1/§2.3/§13.3/§19.13). Transaction/offer-level (base =
/// CustomerPayableServiceAmount), NOT per line. Four models (Percentage/Fixed/PercentageWithBounds/Waived).
/// Resolution reuses the BE-P2 pattern: specificity (CustomerType+Category &gt; CustomerType &gt; Category &gt; Global)
/// then Priority, fail-loud on ties. Produces exactly the values the BE-P1 snapshot expects
/// (<c>PlatformFee{Rate,Minimum,Maximum,Net,Vat,Gross,Base,RuleId}Snapshot</c>); the snapshot write itself is P8.
///
/// Immutable-ish audit entity: all props have private setters, mutated only via the Update/Deactivate methods.
/// Construction only via the validating <see cref="Create"/> factory (enforces model coherence).
/// </summary>
[DocumentationInfo("Platform fee rule entity",
    "Data-driven platform fee (Percentage/Fixed/PercentageWithBounds/Waived). Resolution mirrors the commission " +
    "engine (specificity + Priority + fail-loud conflict). Base = CustomerPayableServiceAmount (§19.13).")]
public sealed class PlatformFeeRuleEntity : AizenEntityWithAudit
{
    // ── Model + amounts ─────────────────────────────────────────────────────────
    public PlatformFeeModel Model        { get; private set; }
    public decimal?         Rate         { get; private set; }   // fraction, e.g. 0.025 = 2.5%
    public decimal?         FixedAmount  { get; private set; }
    public decimal?         MinAmount    { get; private set; }
    public decimal?         MaxAmount    { get; private set; }

    // ── Targeting dimensions ─────────────────────────────────────────────────────
    public string  CurrencyCode { get; private set; } = "TRY";
    public string? CategoryCode { get; private set; }            // null = any category
    public string? CustomerType { get; private set; }            // participant PlanCode (BASIC/GOLD/PLATINUM); null = any

    // ── Lifecycle / admin ────────────────────────────────────────────────────────
    public CommissionRulePriority Priority      { get; private set; }
    public DateTime               EffectiveFrom { get; private set; }
    public DateTime?              EffectiveTo   { get; private set; }   // null = never expires
    public CommissionRuleStatus   Status        { get; private set; }
    public string?                RuleCode      { get; private set; }
    public string?                RuleName      { get; private set; }
    public string?                Notes         { get; private set; }

    /// <summary>
    /// Optional per-rule VAT/KDV rate (fraction). Null → resolved from ReferenceData / configurable default
    /// at apply time (§5). The definitive VAT treatment awaits YMM sign-off (§2.5).
    /// </summary>
    public decimal? VatRate { get; private set; }

    private PlatformFeeRuleEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a validated platform fee rule. Enforces model coherence (§2): Percentage needs Rate; Fixed needs
    /// FixedAmount; PercentageWithBounds needs Rate + Min + Max with Min ≤ Max; Waived needs none.
    /// Throws <see cref="AizenBusinessException"/>(<see cref="PaymentErrorCode.PlatformFeeRuleInvalid"/>) on violation.
    /// </summary>
    public static PlatformFeeRuleEntity Create(
        PlatformFeeModel       model,
        decimal?               rate,
        decimal?               fixedAmount,
        decimal?               minAmount,
        decimal?               maxAmount,
        string                 currencyCode,
        string?                categoryCode,
        string?                customerType,
        CommissionRulePriority priority,
        DateTime               effectiveFrom,
        DateTime?              effectiveTo,
        string?                ruleCode,
        string?                ruleName = null,
        string?                notes    = null,
        decimal?               vatRate  = null)
    {
        ValidateModel(model, rate, fixedAmount, minAmount, maxAmount);

        return new PlatformFeeRuleEntity
        {
            Model         = model,
            Rate          = rate,
            FixedAmount   = fixedAmount,
            MinAmount     = minAmount,
            MaxAmount     = maxAmount,
            CurrencyCode  = currencyCode.ToUpperInvariant(),
            CategoryCode  = categoryCode?.ToUpperInvariant(),
            CustomerType  = customerType?.ToUpperInvariant(),
            Priority      = priority,
            EffectiveFrom = effectiveFrom,
            EffectiveTo   = effectiveTo,
            RuleCode      = ruleCode,
            RuleName      = ruleName,
            Notes         = notes,
            VatRate       = vatRate,
            IsActive      = true,
            Status        = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>Admin update of the mutable economic + lifecycle fields. Re-validates model coherence.</summary>
    public void Update(
        PlatformFeeModel       model,
        decimal?               rate,
        decimal?               fixedAmount,
        decimal?               minAmount,
        decimal?               maxAmount,
        CommissionRulePriority priority,
        DateTime               effectiveFrom,
        DateTime?              effectiveTo,
        string?                ruleName,
        string?                notes,
        decimal?               vatRate)
    {
        ValidateModel(model, rate, fixedAmount, minAmount, maxAmount);

        Model         = model;
        Rate          = rate;
        FixedAmount   = fixedAmount;
        MinAmount     = minAmount;
        MaxAmount     = maxAmount;
        Priority      = priority;
        EffectiveFrom = effectiveFrom;
        EffectiveTo   = effectiveTo;
        RuleName      = ruleName;
        Notes         = notes;
        VatRate       = vatRate;
        Status        = DeriveStatus(effectiveFrom, effectiveTo);
    }

    /// <summary>Sets the targeting scope (CategoryCode / CustomerType) after the factory.</summary>
    public void SetScope(string? categoryCode, string? customerType)
    {
        CategoryCode = categoryCode?.ToUpperInvariant();
        CustomerType = customerType?.ToUpperInvariant();
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    public void Reactivate()
    {
        IsActive = true;
        Status   = DeriveStatus(EffectiveFrom, EffectiveTo);
    }

    public void SetRuleCode(string ruleCode) => RuleCode = ruleCode;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo >= atUtc);

    // ── Validation / helpers ────────────────────────────────────────────────────

    private static void ValidateModel(
        PlatformFeeModel model, decimal? rate, decimal? fixedAmount, decimal? minAmount, decimal? maxAmount)
    {
        switch (model)
        {
            case PlatformFeeModel.Percentage:
                if (rate is null or <= 0m)
                    throw Invalid("Percentage model requires a positive Rate.");
                break;

            case PlatformFeeModel.Fixed:
                if (fixedAmount is null or < 0m)
                    throw Invalid("Fixed model requires a non-negative FixedAmount.");
                break;

            case PlatformFeeModel.PercentageWithBounds:
                if (rate is null or <= 0m)
                    throw Invalid("PercentageWithBounds model requires a positive Rate.");
                if (minAmount is null || maxAmount is null)
                    throw Invalid("PercentageWithBounds model requires both MinAmount and MaxAmount.");
                if (minAmount > maxAmount)
                    throw Invalid("PercentageWithBounds model requires MinAmount ≤ MaxAmount.");
                break;

            case PlatformFeeModel.Waived:
                break;

            default:
                throw Invalid($"Unknown platform fee model '{model}'.");
        }
    }

    private static AizenBusinessException Invalid(string message) =>
        new((int)PaymentErrorCode.PlatformFeeRuleInvalid, message);

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value < now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
