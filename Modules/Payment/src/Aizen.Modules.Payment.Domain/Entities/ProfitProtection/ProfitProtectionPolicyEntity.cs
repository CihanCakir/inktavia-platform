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

    // ── BE-S9 line-level profit-protection defaults (§20.12) — the NON-PART line floor set + the negative-contribution
    //    ban knobs. Part lines take their floors from the S5 PartLineAllowanceDto instead; these apply to every other line.
    //    All admin-tunable, NO hardcoded constant in the engine. Launch defaults are a no-op (caps 100%, floors 0) so an
    //    offer whose lines already clear is byte-identical to pre-S9; admin tightens them. ──
    public decimal DefaultLineMinProviderReceivableRate    { get; private set; }   // × line gross (non-part min-receivable)
    public decimal DefaultLineMinProviderReceivableAmount  { get; private set; }   // floor = Max(amount, gross × rate)
    public decimal DefaultAllowedProviderFundedDiscountRate { get; private set; }  // × line gross (non-part provider-funded cap)
    public decimal DefaultAllowedPlatformFundedDiscountRate { get; private set; }  // × line gross (non-part platform-funded cap)
    public decimal LineCommissionFloorRate                 { get; private set; }   // per-line commission-rate floor (all lines; reuses P7 semantics)
    public decimal MinLinePlatformContributionRate         { get; private set; }   // × line gross → min required line platform contribution (≥0)
    /// <summary>When true, a line may run below its min platform contribution up to <see cref="StrategicLossExceptionMaxLineDeficit"/> (logged, never silent). Default false.</summary>
    public bool    StrategicLossExceptionEnabled           { get; private set; }
    /// <summary>The versioned per-line contribution-deficit an active strategic-loss exception may absorb. 0 = no loss permitted even when enabled.</summary>
    public decimal StrategicLossExceptionMaxLineDeficit    { get; private set; }

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
        string?  policyCode, string? policyName = null, string? notes = null,
        // ── BE-S9 line-level defaults (optional; launch no-op → caps 100%, floors 0, exception off) ──
        decimal  defaultLineMinProviderReceivableRate    = 0m,
        decimal  defaultLineMinProviderReceivableAmount  = 0m,
        decimal  defaultAllowedProviderFundedDiscountRate = 1m,
        decimal  defaultAllowedPlatformFundedDiscountRate = 1m,
        decimal  lineCommissionFloorRate                 = 0m,
        decimal  minLinePlatformContributionRate         = 0m,
        bool     strategicLossExceptionEnabled           = false,
        decimal  strategicLossExceptionMaxLineDeficit    = 0m)
    {
        Validate(
            minCustomerSideAmount, minCustomerSideRate, minProviderSideAmount, minProviderSideRate,
            minTransactionAmount, minTransactionRate, paymentProcessingExpenseRate, paymentProcessingFixed,
            refundRiskReserveRate, otherVariableExpenseRate, otherVariableExpenseFixed,
            customerSideVariableCostShareRate, effectiveFrom, effectiveTo,
            defaultLineMinProviderReceivableRate, defaultLineMinProviderReceivableAmount,
            defaultAllowedProviderFundedDiscountRate, defaultAllowedPlatformFundedDiscountRate,
            lineCommissionFloorRate, minLinePlatformContributionRate, strategicLossExceptionMaxLineDeficit);

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
            DefaultLineMinProviderReceivableRate    = defaultLineMinProviderReceivableRate,
            DefaultLineMinProviderReceivableAmount  = defaultLineMinProviderReceivableAmount,
            DefaultAllowedProviderFundedDiscountRate = defaultAllowedProviderFundedDiscountRate,
            DefaultAllowedPlatformFundedDiscountRate = defaultAllowedPlatformFundedDiscountRate,
            LineCommissionFloorRate           = lineCommissionFloorRate,
            MinLinePlatformContributionRate   = minLinePlatformContributionRate,
            StrategicLossExceptionEnabled     = strategicLossExceptionEnabled,
            StrategicLossExceptionMaxLineDeficit = strategicLossExceptionMaxLineDeficit,
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
        string? policyName, string? notes,
        // ── BE-S9 line-level defaults (optional; unchanged when not supplied) ──
        decimal? defaultLineMinProviderReceivableRate    = null,
        decimal? defaultLineMinProviderReceivableAmount  = null,
        decimal? defaultAllowedProviderFundedDiscountRate = null,
        decimal? defaultAllowedPlatformFundedDiscountRate = null,
        decimal? lineCommissionFloorRate                 = null,
        decimal? minLinePlatformContributionRate         = null,
        bool?    strategicLossExceptionEnabled           = null,
        decimal? strategicLossExceptionMaxLineDeficit    = null)
    {
        // Fall back to the current value when the caller omits a line-level field (keeps the P5 admin update path additive).
        var s9MinRecvRate   = defaultLineMinProviderReceivableRate    ?? DefaultLineMinProviderReceivableRate;
        var s9MinRecvAmount = defaultLineMinProviderReceivableAmount  ?? DefaultLineMinProviderReceivableAmount;
        var s9ProvCapRate   = defaultAllowedProviderFundedDiscountRate ?? DefaultAllowedProviderFundedDiscountRate;
        var s9PlatCapRate   = defaultAllowedPlatformFundedDiscountRate ?? DefaultAllowedPlatformFundedDiscountRate;
        var s9CommFloor     = lineCommissionFloorRate                 ?? LineCommissionFloorRate;
        var s9MinContrib    = minLinePlatformContributionRate         ?? MinLinePlatformContributionRate;
        var s9LossEnabled   = strategicLossExceptionEnabled           ?? StrategicLossExceptionEnabled;
        var s9LossDeficit   = strategicLossExceptionMaxLineDeficit    ?? StrategicLossExceptionMaxLineDeficit;

        Validate(
            minCustomerSideAmount, minCustomerSideRate, minProviderSideAmount, minProviderSideRate,
            minTransactionAmount, minTransactionRate, paymentProcessingExpenseRate, paymentProcessingFixed,
            refundRiskReserveRate, otherVariableExpenseRate, otherVariableExpenseFixed,
            customerSideVariableCostShareRate, effectiveFrom, effectiveTo,
            s9MinRecvRate, s9MinRecvAmount, s9ProvCapRate, s9PlatCapRate,
            s9CommFloor, s9MinContrib, s9LossDeficit);

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
        DefaultLineMinProviderReceivableRate    = s9MinRecvRate;
        DefaultLineMinProviderReceivableAmount  = s9MinRecvAmount;
        DefaultAllowedProviderFundedDiscountRate = s9ProvCapRate;
        DefaultAllowedPlatformFundedDiscountRate = s9PlatCapRate;
        LineCommissionFloorRate           = s9CommFloor;
        MinLinePlatformContributionRate   = s9MinContrib;
        StrategicLossExceptionEnabled     = s9LossEnabled;
        StrategicLossExceptionMaxLineDeficit = s9LossDeficit;
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
        decimal customerSideVariableCostShareRate, DateTime effectiveFrom, DateTime? effectiveTo,
        decimal defaultLineMinProviderReceivableRate, decimal defaultLineMinProviderReceivableAmount,
        decimal defaultAllowedProviderFundedDiscountRate, decimal defaultAllowedPlatformFundedDiscountRate,
        decimal lineCommissionFloorRate, decimal minLinePlatformContributionRate,
        decimal strategicLossExceptionMaxLineDeficit)
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

        // BE-S9 line-level defaults — all ≥ 0 (a rate may exceed 1 only conceptually; caps default to 1 = 100%).
        NonNegative(defaultLineMinProviderReceivableRate,    nameof(defaultLineMinProviderReceivableRate));
        NonNegative(defaultLineMinProviderReceivableAmount,  nameof(defaultLineMinProviderReceivableAmount));
        NonNegative(defaultAllowedProviderFundedDiscountRate, nameof(defaultAllowedProviderFundedDiscountRate));
        NonNegative(defaultAllowedPlatformFundedDiscountRate, nameof(defaultAllowedPlatformFundedDiscountRate));
        NonNegative(lineCommissionFloorRate,                 nameof(lineCommissionFloorRate));
        NonNegative(minLinePlatformContributionRate,         nameof(minLinePlatformContributionRate));
        NonNegative(strategicLossExceptionMaxLineDeficit,    nameof(strategicLossExceptionMaxLineDeficit));

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
