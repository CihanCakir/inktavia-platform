using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// BE-S9 (§20.12) — the pure LINE-level profit-protection engine. Runs BEFORE the transaction-level
/// <see cref="ProfitProtectionEngine"/>: each line must independently clear its own floor, so a line's loss can NOT be
/// hidden inside another line's profit (<b>no netting</b>). NO writes, NO config, NO hardcoded rate/amount constant — every
/// threshold comes from the resolved <see cref="ProfitProtectionPolicyEntity"/> (non-part lines) or the S5
/// <see cref="LinePartAllowance"/> (part lines). Deterministic + exact decimal (<see cref="MoneyMath"/>).
///
/// <para>Per line, in order, the first breach fails the line (§20.12):
/// <list type="number">
/// <item>Provider min-receivable: <c>ProviderNet ≥ providerMinimumReceivable</c>.</item>
/// <item>Funded-discount caps: <c>ProviderFundedDiscount ≤ allowedProviderFundedDiscount</c> AND
///       <c>PlatformFundedDiscount ≤ allowedPlatformFundedDiscount</c>.</item>
/// <item>Commission floor: <c>ResolvedRate ≥ LineCommissionFloorRate</c> (reuses the P7 ProviderCommissionBelowFloor contract).</item>
/// <item>Line platform contribution (negative-contribution ban): <c>CommissionNetRevenue + proRataPlatformFeeNetRevenue −
///       PlatformFundedDiscount − proRataProviderCommissionBenefitCost ≥ minLinePlatformContribution</c> — a breach fails
///       UNLESS <c>StrategicLossExceptionEnabled</c> and the deficit is within <c>StrategicLossExceptionMaxLineDeficit</c>.</item>
/// </list>
/// The transaction platform fee net and provider-commission-benefit cost are attributed pro-rata by line commission base
/// (documented allocation; reporting-only — it does NOT alter the transaction fee).</para>
/// </summary>
public static class LineProfitProtectionEngine
{
    public static LineProfitProtectionEvaluation Evaluate(
        IReadOnlyList<LineProfitProtectionLineInput> lines,
        ProfitProtectionPolicyEntity? policy,
        IReadOnlyDictionary<string, LinePartAllowance> partAllowances,
        decimal transactionPlatformFeeNetRevenue,
        decimal transactionProviderCommissionBenefitCost)
    {
        // Missing policy → ConfigurationError (mirrors the transaction engine; the combiner blocks acceptance).
        if (policy is null)
            return new LineProfitProtectionEvaluation(
                Passed: false, State: ProfitProtectionDecisionState.ConfigurationError,
                Lines: [], PrimaryErrorCode: (int)PaymentErrorCode.LineProfitProtectionConfigurationError,
                Reason: "No active ProfitProtectionPolicy resolved — line-level floors cannot be applied.");

        // Pro-rata denominator = Σ line commission base (weights the transaction platform fee / benefit onto the lines).
        decimal totalCommissionBase = 0m;
        foreach (var l in lines) totalCommissionBase += l.CommissionBase;

        var results = new List<LineProfitProtectionResult>(lines.Count);
        var hasConfigError = false;
        var hasBreach = false;
        int? primaryCode = null;
        string? primaryReason = null;

        foreach (var line in lines)
        {
            // ── Resolve the floor set for this line ──
            decimal minReceivable, allowedProviderFunded, allowedPlatformFunded;
            if (line.IsPartLine)
            {
                if (!partAllowances.TryGetValue(line.LineRef, out var allowance) || !allowance.Found)
                {
                    var configReason = $"Line '{line.LineRef}': part line has no resolved S5 commercial-term allowance " +
                                       "(cannot apply its provider-minimum-receivable floor).";
                    results.Add(new LineProfitProtectionResult(
                        line.LineRef, Passed: false, LineProfitProtectionBreach.MissingAllowance,
                        ProviderMinimumReceivableApplied: 0m, LinePlatformContribution: 0m, MinLinePlatformContribution: 0m,
                        StrategicLossExceptionApplied: false,
                        ErrorCode: (int)PaymentErrorCode.LineProfitProtectionConfigurationError, Reason: configReason));
                    hasConfigError = true;
                    primaryCode ??= (int)PaymentErrorCode.LineProfitProtectionConfigurationError;
                    primaryReason ??= configReason;
                    continue;
                }
                minReceivable         = allowance.MinimumProviderReceivable;
                allowedProviderFunded = allowance.AllowedProviderFundedDiscount;
                allowedPlatformFunded = allowance.AllowedPlatformFundedDiscount;
            }
            else
            {
                minReceivable = Max(
                    policy.DefaultLineMinProviderReceivableAmount,
                    MoneyMath.Round(line.LineBase * policy.DefaultLineMinProviderReceivableRate));
                allowedProviderFunded = MoneyMath.Round(line.LineBase * policy.DefaultAllowedProviderFundedDiscountRate);
                allowedPlatformFunded = MoneyMath.Round(line.LineBase * policy.DefaultAllowedPlatformFundedDiscountRate);
            }

            var minLineContribution = Max(0m, MoneyMath.Round(line.LineBase * policy.MinLinePlatformContributionRate));

            // ── Line platform contribution (check 4 input; always computed for the descriptive record) ──
            var weight = totalCommissionBase > 0m ? line.CommissionBase / totalCommissionBase : 0m;
            var proRataFee     = MoneyMath.Round(transactionPlatformFeeNetRevenue * weight);
            var proRataBenefit = MoneyMath.Round(transactionProviderCommissionBenefitCost * weight);
            var lineContribution = line.CommissionNetRevenue + proRataFee - line.PlatformFundedDiscount - proRataBenefit;

            // ── Checks, in order; first breach fails the line ──
            LineProfitProtectionBreach breach = LineProfitProtectionBreach.None;
            bool lossExceptionApplied = false;
            int? code = null;
            string? reason = null;

            if (line.ProviderNet < minReceivable)
            {
                breach = LineProfitProtectionBreach.ProviderReceivableBelowFloor;
                code = (int)PaymentErrorCode.LineProfitProtectionProviderReceivableBelowFloor;
                reason = $"Line '{line.LineRef}': provider net {line.ProviderNet} below minimum receivable {minReceivable}.";
            }
            else if (line.ProviderFundedDiscount > allowedProviderFunded || line.PlatformFundedDiscount > allowedPlatformFunded)
            {
                breach = LineProfitProtectionBreach.FundedDiscountExceedsCap;
                code = (int)PaymentErrorCode.LineProfitProtectionFundedDiscountExceedsCap;
                reason = $"Line '{line.LineRef}': funded discount over cap (provider {line.ProviderFundedDiscount}/{allowedProviderFunded}, " +
                         $"platform {line.PlatformFundedDiscount}/{allowedPlatformFunded}).";
            }
            else if (line.ResolvedRate < policy.LineCommissionFloorRate)
            {
                breach = LineProfitProtectionBreach.CommissionBelowFloor;
                // Reuse the P7 contract code — a line-level commission-floor breach IS a ProviderCommissionBelowFloor.
                code = (int)PaymentErrorCode.ProviderCommissionBelowFloor;
                reason = $"Line '{line.LineRef}': commission rate {line.ResolvedRate} below the line floor {policy.LineCommissionFloorRate}.";
            }
            else if (lineContribution < minLineContribution)
            {
                var deficit = minLineContribution - lineContribution;
                if (policy.StrategicLossExceptionEnabled && deficit <= policy.StrategicLossExceptionMaxLineDeficit)
                {
                    lossExceptionApplied = true;   // allowed under the versioned exception — logged, not a breach
                    reason = $"Line '{line.LineRef}': platform contribution {lineContribution} below min {minLineContribution} " +
                             $"— allowed under strategic-loss exception (deficit {deficit} ≤ {policy.StrategicLossExceptionMaxLineDeficit}).";
                }
                else
                {
                    breach = LineProfitProtectionBreach.NegativeContribution;
                    code = (int)PaymentErrorCode.LineProfitProtectionNegativeContribution;
                    reason = $"Line '{line.LineRef}': platform contribution {lineContribution} below min {minLineContribution} " +
                             (policy.StrategicLossExceptionEnabled
                                 ? $"(deficit {deficit} exceeds the strategic-loss limit {policy.StrategicLossExceptionMaxLineDeficit})."
                                 : "(strategic-loss exception disabled).");
                }
            }

            var passed = breach == LineProfitProtectionBreach.None;
            if (!passed)
            {
                hasBreach = true;
                primaryCode ??= code;
                primaryReason ??= reason;
            }

            results.Add(new LineProfitProtectionResult(
                line.LineRef, passed, breach,
                ProviderMinimumReceivableApplied: minReceivable,
                LinePlatformContribution: MoneyMath.Round(lineContribution),
                MinLinePlatformContribution: minLineContribution,
                StrategicLossExceptionApplied: lossExceptionApplied,
                ErrorCode: passed ? null : code,
                Reason: reason));
        }

        // ── Overall (no netting): any ConfigurationError line → ConfigurationError; else any breach → Rejected ──
        if (hasConfigError)
            return new LineProfitProtectionEvaluation(
                false, ProfitProtectionDecisionState.ConfigurationError, results, primaryCode, primaryReason);
        if (hasBreach)
            return new LineProfitProtectionEvaluation(
                false, ProfitProtectionDecisionState.Rejected, results,
                primaryCode ?? (int)PaymentErrorCode.LineProfitProtectionRejected, primaryReason);

        return new LineProfitProtectionEvaluation(
            true, ProfitProtectionDecisionState.Approved, results, null, null);
    }

    private static decimal Max(decimal a, decimal b) => a >= b ? a : b;
}
