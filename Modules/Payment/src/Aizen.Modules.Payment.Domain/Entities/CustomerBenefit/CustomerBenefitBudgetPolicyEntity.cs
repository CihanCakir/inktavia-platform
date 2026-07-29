using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

/// <summary>
/// Per-plan, versioned policy that sizes and bounds a customer's benefit budget (§19.7). <c>BenefitBudgetRate</c> is the
/// fraction of plan-period revenue funneled into the budget (so ELITE ≠ unlimited); optional per-period / per-category /
/// per-transaction limits; and the refund-restore behaviour (restore hook wired in P10). Single-active per (plan, currency).
/// </summary>
[DocumentationInfo("Customer benefit budget policy entity",
    "Per-plan versioned policy: BenefitBudgetRate (fraction of plan revenue → budget; ELITE ≠ unlimited), optional " +
    "per-period/category/transaction limits, refund-restore policy.")]
public sealed class CustomerBenefitBudgetPolicyEntity : AizenEntityWithAudit
{
    public long                       CustomerPlanId      { get; private set; }
    public string                     CurrencyCode        { get; private set; } = "TRY";
    public decimal                    BenefitBudgetRate   { get; private set; }   // fraction of plan-period revenue
    public decimal?                   PerPeriodMax        { get; private set; }
    public decimal?                   PerCategoryLimit    { get; private set; }
    public decimal?                   PerTransactionLimit { get; private set; }
    public BenefitRefundRestorePolicy RefundRestorePolicy { get; private set; }

    public DateTime             EffectiveFrom { get; private set; }
    public DateTime?            EffectiveTo   { get; private set; }
    public CommissionRuleStatus Status        { get; private set; }
    public string?              PolicyCode    { get; private set; }
    public string?              Notes         { get; private set; }

    private CustomerBenefitBudgetPolicyEntity() { }

    public static CustomerBenefitBudgetPolicyEntity Create(
        long customerPlanId, string currencyCode, decimal benefitBudgetRate,
        decimal? perPeriodMax, decimal? perCategoryLimit, decimal? perTransactionLimit,
        BenefitRefundRestorePolicy refundRestorePolicy,
        DateTime effectiveFrom, DateTime? effectiveTo, string? policyCode, string? notes = null)
    {
        Validate(benefitBudgetRate, perPeriodMax, perCategoryLimit, perTransactionLimit, effectiveFrom, effectiveTo);

        return new CustomerBenefitBudgetPolicyEntity
        {
            CustomerPlanId      = customerPlanId,
            CurrencyCode        = currencyCode.ToUpperInvariant(),
            BenefitBudgetRate   = benefitBudgetRate,
            PerPeriodMax        = perPeriodMax,
            PerCategoryLimit    = perCategoryLimit,
            PerTransactionLimit = perTransactionLimit,
            RefundRestorePolicy = refundRestorePolicy,
            EffectiveFrom       = effectiveFrom,
            EffectiveTo         = effectiveTo,
            PolicyCode          = policyCode,
            Notes               = notes,
            IsActive            = true,
            Status              = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    public void Update(decimal benefitBudgetRate, decimal? perPeriodMax, decimal? perCategoryLimit,
        decimal? perTransactionLimit, BenefitRefundRestorePolicy refundRestorePolicy,
        DateTime effectiveFrom, DateTime? effectiveTo, string? notes)
    {
        Validate(benefitBudgetRate, perPeriodMax, perCategoryLimit, perTransactionLimit, effectiveFrom, effectiveTo);
        BenefitBudgetRate   = benefitBudgetRate;
        PerPeriodMax        = perPeriodMax;
        PerCategoryLimit    = perCategoryLimit;
        PerTransactionLimit = perTransactionLimit;
        RefundRestorePolicy = refundRestorePolicy;
        EffectiveFrom       = effectiveFrom;
        EffectiveTo         = effectiveTo;
        Notes               = notes;
        Status              = DeriveStatus(effectiveFrom, effectiveTo);
    }

    public void Deactivate()
    {
        IsActive = false;
        Status   = CommissionRuleStatus.Inactive;
    }

    public void SetPolicyCode(string policyCode) => PolicyCode = policyCode;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    /// <summary>Sizes a period budget: Round(planPeriodRevenue × BenefitBudgetRate), capped by <see cref="PerPeriodMax"/>.</summary>
    public decimal ComputeFundedAmount(decimal planPeriodRevenue)
    {
        var funded = MoneyMath.Round(planPeriodRevenue * BenefitBudgetRate);
        if (PerPeriodMax is { } max && funded > max) funded = max;
        return funded < 0m ? 0m : funded;
    }

    private static void Validate(decimal rate, decimal? perPeriodMax, decimal? perCategoryLimit,
        decimal? perTransactionLimit, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        AizenBusinessException Invalid(string m) => new((int)PaymentErrorCode.CustomerBenefitBudgetPolicyConflict, m);
        if (rate < 0m || rate > 1m) throw Invalid("BenefitBudgetRate must be within [0, 1] (the whole plan fee cannot be the budget).");
        if (perPeriodMax is < 0m) throw Invalid("PerPeriodMax must be ≥ 0.");
        if (perCategoryLimit is < 0m) throw Invalid("PerCategoryLimit must be ≥ 0.");
        if (perTransactionLimit is < 0m) throw Invalid("PerTransactionLimit must be ≥ 0.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom) throw Invalid("EffectiveTo must be after EffectiveFrom.");
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
