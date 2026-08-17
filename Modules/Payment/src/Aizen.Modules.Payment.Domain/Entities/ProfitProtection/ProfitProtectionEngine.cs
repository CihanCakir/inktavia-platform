using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Pure profit-protection engine (§19.1–19.3, §19.10, §19.11) — context in → decision out. NO writes, NO config,
/// NO hardcoded rate/amount constants: every threshold comes from the resolved <see cref="ProfitProtectionPolicyEntity"/>.
/// Deterministic: the same (context, policy) always yields the same evaluation.
///
/// <para>Computes the three contributions (§19.2), each Required = Max(amount, base×rate) (§19.3), the expected
/// variable expenses derived from the policy, the safe-max platform-funded discount (§19.10), then adjusts the
/// requested advantages down (per <c>AdjustmentOrder</c>) to the maximum that keeps ALL three gates satisfied with
/// zero tolerance, producing one of Approved / ApprovedWithAdjustment / Rejected / ConfigurationError (§19.11).</para>
///
/// <para><b>Contribution bases</b> (open decision, §19 — sensible documented defaults): transaction &amp; customer-side
/// bases = CustomerTotalAmount; provider-side base = ServiceAmount. <b>Variable-cost split</b> to customer/provider
/// side is policy-configurable (<c>CustomerSideVariableCostShareRate</c>).</para>
/// </summary>
public static class ProfitProtectionEngine
{
    public static ProfitProtectionEvaluation Evaluate(ProfitProtectionContext ctx, ProfitProtectionPolicyEntity? policy)
    {
        if (policy is null)
            return ConfigurationError(policyId: null,
                "No active ProfitProtectionPolicy resolved for the currency (missing/conflicting policy).");

        // ── Expected variable expenses — derived from the policy (§4), not passed in ──
        var processing = MoneyMath.Round(ctx.CustomerTotalAmount * policy.PaymentProcessingExpenseRate) + policy.PaymentProcessingFixed;
        var refund     = MoneyMath.Round(ctx.CustomerTotalAmount * policy.RefundRiskReserveRate);
        var other      = MoneyMath.Round(ctx.CustomerTotalAmount * policy.OtherVariableExpenseRate) + policy.OtherVariableExpenseFixed;
        var totalExpenses   = processing + refund + other;
        var customerVarCost = MoneyMath.Round(totalExpenses * policy.CustomerSideVariableCostShareRate);
        var providerVarCost = totalExpenses - customerVarCost;

        // ── Required contributions (§19.3): Required = Max(amount, base × rate) ──
        var reqCustomer    = Max(policy.MinCustomerSideContributionAmount, MoneyMath.Round(ctx.CustomerTotalAmount * policy.MinCustomerSideContributionRate));
        var reqProvider    = Max(policy.MinProviderSideContributionAmount, MoneyMath.Round(ctx.ServiceAmount       * policy.MinProviderSideContributionRate));
        var reqTransaction = Max(policy.MinTransactionContributionAmount,  MoneyMath.Round(ctx.CustomerTotalAmount * policy.MinTransactionContributionRate));

        // ── Revenue aggregates ──
        var custRevenue     = ctx.CustomerPlatformFeeNetRevenue + ctx.CustomerPlanRevenueAllocation + ctx.CustomerPremiumRevenueAllocation;
        var providerRevenue = ctx.ProviderCommissionNetRevenue  + ctx.ProviderPlanRevenueAllocation + ctx.ProviderAddOnRevenueAllocation;
        var subscriptionAllocations = ctx.CustomerPlanRevenueAllocation + ctx.CustomerPremiumRevenueAllocation + ctx.ProviderPlanRevenueAllocation;
        var addOnAllocations        = ctx.ProviderAddOnRevenueAllocation;
        var txnRevenue = ctx.ProviderCommissionNetRevenue + ctx.CustomerPlatformFeeNetRevenue + subscriptionAllocations + addOnAllocations;

        // Contribution functions (§19.2) of the applied advantages:
        decimal CustomerSide(decimal discount) => custRevenue     - discount - customerVarCost;
        decimal ProviderSide(decimal benefit)  => providerRevenue - benefit  - providerVarCost;
        decimal Total(decimal discount)        => txnRevenue      - discount - totalExpenses;

        var requestedDiscount = ctx.RequestedPlatformFundedCustomerDiscount;
        var requestedBenefit  = ctx.ProviderCommissionBenefitCost;

        // ── Safe-max platform-funded discount (§19.10, total-gate based) ──
        // PreDiscountExpectedContribution − RequiredTransactionContribution − ExpectedVariableExpenses,
        // where PreDiscount = pre-discount, pre-expense revenue contribution = txnRevenue  →  == Total(0) − reqTransaction.
        var safeMaxTotal = Max(0m, txnRevenue - reqTransaction - totalExpenses);
        // Must keep CustomerTotalAmount ≥ ProviderNetAmount: reconstruct the pre-discount total and bound the extra room.
        var customerTotalAtZeroDiscount = ctx.CustomerTotalAmount + requestedDiscount;
        var netConstraint = Max(0m, customerTotalAtZeroDiscount - ctx.ProviderNetAmount);
        // Clamp to the customer benefit budget (§19.10).
        var maximumSafeDiscount = Max(0m, Min(safeMaxTotal, Min(netConstraint, ctx.CustomerBenefitBudgetRemaining)));

        // ── Apply advantages down to the maximum that keeps EVERY gate safe (zero tolerance) ──
        // Discount also bounded by the customer-side gate; provider gate is discount-independent (uses the benefit lever).
        var customerDiscountBound = custRevenue - customerVarCost - reqCustomer;   // max discount for the customer gate
        var discountCap    = Max(0m, Min(maximumSafeDiscount, customerDiscountBound));
        var appliedDiscount = Min(requestedDiscount, discountCap);
        if (appliedDiscount < 0m) appliedDiscount = 0m;

        var benefitBound   = Max(0m, providerRevenue - providerVarCost - reqProvider);  // max benefit for the provider gate
        var appliedBenefit = Min(requestedBenefit, benefitBound);
        if (appliedBenefit < 0m) appliedBenefit = 0m;

        // ── Evaluate the three gates at the applied advantages (§19.2, exact decimal) ──
        var customerContribution = CustomerSide(appliedDiscount);
        var providerContribution = ProviderSide(appliedBenefit);
        var totalContribution    = Total(appliedDiscount);

        var allGatesPass = customerContribution >= reqCustomer
                        && providerContribution >= reqProvider
                        && totalContribution    >= reqTransaction;

        ProfitProtectionDecisionState state;
        string? reason = null;

        if (allGatesPass)
        {
            var adjusted = appliedDiscount != requestedDiscount || appliedBenefit != requestedBenefit;
            state = adjusted ? ProfitProtectionDecisionState.ApprovedWithAdjustment
                             : ProfitProtectionDecisionState.Approved;
            if (adjusted)
                reason = BuildAdjustmentReason(policy.AdjustmentOrder,
                    requestedDiscount, appliedDiscount, requestedBenefit, appliedBenefit);
        }
        else
        {
            state  = ProfitProtectionDecisionState.Rejected;
            reason = "No safe combination of advantages meets the minimum contributions (§19.2): " +
                     $"customer {customerContribution}/{reqCustomer}, provider {providerContribution}/{reqProvider}, " +
                     $"transaction {totalContribution}/{reqTransaction}.";
        }

        return new ProfitProtectionEvaluation(
            State:                                state,
            PolicyId:                             policy.Id,
            AdjustmentReason:                     reason,
            AppliedPlatformFundedDiscount:        appliedDiscount,
            AppliedCommissionBenefit:             appliedBenefit,
            CustomerSideContributionExpected:     customerContribution,
            ProviderSideContributionExpected:     providerContribution,
            TotalTransactionContributionExpected: totalContribution,
            RequiredCustomerSideContribution:     reqCustomer,
            RequiredProviderSideContribution:     reqProvider,
            RequiredTransactionContribution:      reqTransaction,
            ExpectedPaymentProcessingExpense:     processing,
            ExpectedRefundRiskReserve:            refund,
            ExpectedOtherVariableExpenses:        other,
            MaximumSafePlatformFundedDiscount:    maximumSafeDiscount);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ProfitProtectionEvaluation ConfigurationError(long? policyId, string reason)
        => new(ProfitProtectionDecisionState.ConfigurationError, policyId, reason,
               0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

    private static string BuildAdjustmentReason(
        ProfitProtectionAdjustmentOrder order,
        decimal requestedDiscount, decimal appliedDiscount,
        decimal requestedBenefit,  decimal appliedBenefit)
    {
        var parts = new List<string>();
        void Discount() { if (appliedDiscount != requestedDiscount) parts.Add($"platform-funded discount {requestedDiscount}→{appliedDiscount}"); }
        void Benefit()  { if (appliedBenefit  != requestedBenefit)  parts.Add($"commission benefit {requestedBenefit}→{appliedBenefit}"); }

        if (order == ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit) { Discount(); Benefit(); }
        else                                                                                { Benefit(); Discount(); }

        return "Reduced to the safe maximum (§19.10/§19.11): " + string.Join("; ", parts) + ".";
    }

    private static decimal Max(decimal a, decimal b) => a >= b ? a : b;
    private static decimal Min(decimal a, decimal b) => a <= b ? a : b;
}
