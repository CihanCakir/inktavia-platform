using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Versioned, single-active (per currency) profit-protection policy (§19.3). Holds EVERY threshold the engine needs —
/// the three minimum contributions (amount AND rate), the expected-variable-expense rates, the customer/provider
/// variable-cost split, and the adjustment order. <b>No rate/amount is hardcoded in the engine</b> (§19.3); all values
/// live here and are admin-configurable + effective-dated (BE-P2/P4 pattern). Resolution: exactly one Active policy per
/// currency at any instant; overlap → fail-loud conflict.
/// </summary>
[DocumentationInfo("Profit protection policy entity",
    "Versioned admin policy carrying the three minimum contributions (amount+rate), expected-expense rates, " +
    "variable-cost split, and adjustment order for the ProfitProtectionEngine. No hardcoded constants (§19.3).")]
public sealed class ProfitProtectionPolicyEntity : AizenEntityWithAudit
{
    // ── Minimum contributions (§19.3 — amount AND rate) ─────────────────────────
    public decimal MinCustomerSideContributionAmount { get; private set; }
    public decimal MinCustomerSideContributionRate   { get; private set; }
    public decimal MinProviderSideContributionAmount { get; private set; }
    public decimal MinProviderSideContributionRate   { get; private set; }
    public decimal MinTransactionContributionAmount  { get; private set; }
    public decimal MinTransactionContributionRate    { get; private set; }

    // ── Expected variable expense policy (§19.2/§4) ─────────────────────────────
    public decimal PaymentProcessingExpenseRate { get; private set; }   // on CustomerTotalAmount
    public decimal PaymentProcessingFixed       { get; private set; }
    public decimal RefundRiskReserveRate        { get; private set; }   // on CustomerTotalAmount
    public decimal OtherVariableExpenseRate     { get; private set; }   // on CustomerTotalAmount
    public decimal OtherVariableExpenseFixed    { get; private set; }

    /// <summary>
    /// Fraction of the total expected variable expenses allocated to the CUSTOMER side (0..1); the provider side
    /// gets the remainder. Policy-configurable — the exact split is an open decision (§19 open items); default 0.5.
    /// </summary>
    public decimal CustomerSideVariableCostShareRate { get; private set; }

    // ── Adjustment order (§19.11) ───────────────────────────────────────────────
    public ProfitProtectionAdjustmentOrder AdjustmentOrder { get; private set; }

    // ── Lifecycle / admin ────────────────────────────────────────────────────────
    public string               CurrencyCode  { get; private set; } = "TRY";
    public DateTime             EffectiveFrom { get; private set; }
    public DateTime?            EffectiveTo   { get; private set; }
    public CommissionRuleStatus Status        { get; private set; }
    public string?              PolicyCode    { get; private set; }
    public string?              PolicyName    { get; private set; }
    public string?              Notes         { get; private set; }

    private ProfitProtectionPolicyEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ProfitProtectionPolicyEntity Create(
        string   currencyCode,
        decimal  minCustomerSideAmount, decimal minCustomerSideRate,
        decimal  minProviderSideAmount, decimal minProviderSideRate,
        decimal  minTransactionAmount,  decimal minTransactionRate,
        decimal  paymentProcessingExpenseRate, decimal paymentProcessingFixed,
        decimal  refundRiskReserveRate,
        decimal  otherVariableExpenseRate, decimal otherVariableExpenseFixed,
        decimal  customerSideVariableCostShareRate,
        ProfitProtectionAdjustmentOrder adjustmentOrder,
        DateTime effectiveFrom, DateTime? effectiveTo,
        string?  policyCode, string? policyName = null, string? notes = null)
    {
        Validate(
            minCustomerSideAmount, minCustomerSideRate, minProviderSideAmount, minProviderSideRate,
            minTransactionAmount, minTransactionRate, paymentProcessingExpenseRate, paymentProcessingFixed,
            refundRiskReserveRate, otherVariableExpenseRate, otherVariableExpenseFixed,
            customerSideVariableCostShareRate, effectiveFrom, effectiveTo);

        return new ProfitProtectionPolicyEntity
        {
            CurrencyCode                      = currencyCode.ToUpperInvariant(),
            MinCustomerSideContributionAmount = minCustomerSideAmount,
            MinCustomerSideContributionRate   = minCustomerSideRate,
            MinProviderSideContributionAmount = minProviderSideAmount,
            MinProviderSideContributionRate   = minProviderSideRate,
            MinTransactionContributionAmount  = minTransactionAmount,
            MinTransactionContributionRate    = minTransactionRate,
            PaymentProcessingExpenseRate      = paymentProcessingExpenseRate,
            PaymentProcessingFixed            = paymentProcessingFixed,
            RefundRiskReserveRate             = refundRiskReserveRate,
            OtherVariableExpenseRate          = otherVariableExpenseRate,
            OtherVariableExpenseFixed         = otherVariableExpenseFixed,
            CustomerSideVariableCostShareRate = customerSideVariableCostShareRate,
            AdjustmentOrder                   = adjustmentOrder,
            EffectiveFrom                     = effectiveFrom,
            EffectiveTo                       = effectiveTo,
            PolicyCode                        = policyCode,
            PolicyName                        = policyName,
            Notes                             = notes,
            IsActive                          = true,
            Status                            = DeriveStatus(effectiveFrom, effectiveTo),
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        decimal minCustomerSideAmount, decimal minCustomerSideRate,
        decimal minProviderSideAmount, decimal minProviderSideRate,
        decimal minTransactionAmount,  decimal minTransactionRate,
        decimal paymentProcessingExpenseRate, decimal paymentProcessingFixed,
        decimal refundRiskReserveRate,
        decimal otherVariableExpenseRate, decimal otherVariableExpenseFixed,
        decimal customerSideVariableCostShareRate,
        ProfitProtectionAdjustmentOrder adjustmentOrder,
        DateTime effectiveFrom, DateTime? effectiveTo,
        string? policyName, string? notes)
    {
        Validate(
            minCustomerSideAmount, minCustomerSideRate, minProviderSideAmount, minProviderSideRate,
            minTransactionAmount, minTransactionRate, paymentProcessingExpenseRate, paymentProcessingFixed,
            refundRiskReserveRate, otherVariableExpenseRate, otherVariableExpenseFixed,
            customerSideVariableCostShareRate, effectiveFrom, effectiveTo);

        MinCustomerSideContributionAmount = minCustomerSideAmount;
        MinCustomerSideContributionRate   = minCustomerSideRate;
        MinProviderSideContributionAmount = minProviderSideAmount;
        MinProviderSideContributionRate   = minProviderSideRate;
        MinTransactionContributionAmount  = minTransactionAmount;
        MinTransactionContributionRate    = minTransactionRate;
        PaymentProcessingExpenseRate      = paymentProcessingExpenseRate;
        PaymentProcessingFixed            = paymentProcessingFixed;
        RefundRiskReserveRate             = refundRiskReserveRate;
        OtherVariableExpenseRate          = otherVariableExpenseRate;
        OtherVariableExpenseFixed         = otherVariableExpenseFixed;
        CustomerSideVariableCostShareRate = customerSideVariableCostShareRate;
        AdjustmentOrder                   = adjustmentOrder;
        EffectiveFrom                     = effectiveFrom;
        EffectiveTo                       = effectiveTo;
        PolicyName                        = policyName;
        Notes                             = notes;
        Status                            = DeriveStatus(effectiveFrom, effectiveTo);
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

    public void SetPolicyCode(string policyCode) => PolicyCode = policyCode;

    public bool IsEffective(DateTime atUtc) =>
        IsActive && EffectiveFrom <= atUtc && (EffectiveTo == null || EffectiveTo > atUtc);

    // ── Validation / helpers ────────────────────────────────────────────────────

    private static void Validate(
        decimal minCustomerSideAmount, decimal minCustomerSideRate,
        decimal minProviderSideAmount, decimal minProviderSideRate,
        decimal minTransactionAmount,  decimal minTransactionRate,
        decimal paymentProcessingExpenseRate, decimal paymentProcessingFixed,
        decimal refundRiskReserveRate, decimal otherVariableExpenseRate, decimal otherVariableExpenseFixed,
        decimal customerSideVariableCostShareRate, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        void NonNegative(decimal v, string n)
        {
            if (v < 0m) throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyInvalid, $"{n} must be ≥ 0.");
        }

        NonNegative(minCustomerSideAmount, nameof(minCustomerSideAmount));
        NonNegative(minCustomerSideRate,   nameof(minCustomerSideRate));
        NonNegative(minProviderSideAmount, nameof(minProviderSideAmount));
        NonNegative(minProviderSideRate,   nameof(minProviderSideRate));
        NonNegative(minTransactionAmount,  nameof(minTransactionAmount));
        NonNegative(minTransactionRate,    nameof(minTransactionRate));
        NonNegative(paymentProcessingExpenseRate, nameof(paymentProcessingExpenseRate));
        NonNegative(paymentProcessingFixed,       nameof(paymentProcessingFixed));
        NonNegative(refundRiskReserveRate,        nameof(refundRiskReserveRate));
        NonNegative(otherVariableExpenseRate,     nameof(otherVariableExpenseRate));
        NonNegative(otherVariableExpenseFixed,    nameof(otherVariableExpenseFixed));

        if (customerSideVariableCostShareRate is < 0m or > 1m)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyInvalid,
                "CustomerSideVariableCostShareRate must be within [0, 1].");

        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyInvalid, "EffectiveTo must be after EffectiveFrom.");
    }

    private static CommissionRuleStatus DeriveStatus(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var now = DateTime.UtcNow;
        if (effectiveFrom > now) return CommissionRuleStatus.Scheduled;
        if (effectiveTo.HasValue && effectiveTo.Value <= now) return CommissionRuleStatus.Expired;
        return CommissionRuleStatus.Active;
    }
}
