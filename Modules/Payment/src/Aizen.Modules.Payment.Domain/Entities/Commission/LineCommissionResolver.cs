using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.Commission;

/// <summary>Provider identity shared across an offer's lines (BE-S7).</summary>
public sealed record LineCommissionProviderContext(
    long    ProviderProfileId,
    long?   ProviderPlanId,
    string  CurrencyCode);

/// <summary>A single priced offer line to resolve (BE-S7 §1).</summary>
public sealed record LineCommissionInput(
    string                    LineRef,
    LineType?                 LineType,
    string?                   ProductCode,
    LineCommissionEligibility CommissionEligibility,
    string?                   CategoryCode,
    decimal                   CommissionBaseAmount,
    decimal                   LineProviderRevenue);

/// <summary>Per-line resolved commission (BE-S7 §1).</summary>
public sealed record LineCommissionResult(
    string   LineRef,
    bool     Commissionable,
    decimal  CommissionBaseAmount,
    decimal  ResolvedRate,
    string?  RuleCode,
    decimal  CommissionAmount,
    decimal  ProviderNet);

/// <summary>Line results + transaction aggregates (§20.15).</summary>
public sealed record LineCommissionResolutionResult(
    IReadOnlyList<LineCommissionResult> Lines,
    decimal TransactionCommission,
    decimal TransactionProviderNet,
    decimal TransactionCommissionBase,
    string  CurrencyCode);

/// <summary>
/// Pure line-set commission resolution (§20.11) — EXTENDS BE-P2 by composing <see cref="CommissionRuleResolver.Resolve"/>
/// per line over ONE active-rule set. Base rates are NOT re-resolved differently; the single-line primitive and its
/// fail-loud conflict are reused verbatim. No writes, no <c>MarkApplied</c> (applied-count is P8). Deterministic.
///
/// <para>Per line: Exempt → not commissionable (rate 0, commission 0, provider keeps the full line);
/// Eligible → resolve with the <c>CommissionEligibility=Commissionable</c> dimension; InheritFromCategory → resolve by
/// category/plan/global (no eligibility override). <c>CommissionAmount = Round(CommissionBaseAmount × ResolvedRate)</c>,
/// <c>ProviderNet = LineProviderRevenue − CommissionAmount</c>. Commission is rounded PER LINE then summed (§13.6) — the
/// transaction totals are the sum of the rounded line amounts (no blended-rate re-round), so P8/S8 reconcile exactly.</para>
///
/// <para><b>Boundary (control):</b> only <c>CommissionRule</c> data feeds this; premium/boost is never read. If no rule
/// resolves for an eligible/inherit line (e.g. no Global seeded), rate 0 / commission 0 is returned for preview safety —
/// P8 enforces configuration completeness.</para>
/// </summary>
public static class LineCommissionResolver
{
    public static LineCommissionResolutionResult Resolve(
        IEnumerable<CommissionRuleEntity> activeRules,
        LineCommissionProviderContext providerContext,
        IReadOnlyList<LineCommissionInput> lines)
    {
        var rules = activeRules as IReadOnlyCollection<CommissionRuleEntity> ?? activeRules.ToList();

        var results = new List<LineCommissionResult>(lines.Count);
        decimal totalCommission = 0m, totalProviderNet = 0m, totalBase = 0m;

        foreach (var line in lines)
        {
            LineCommissionResult result;

            if (line.CommissionEligibility == LineCommissionEligibility.Exempt)
            {
                // Pass-through / non-commissionable: provider keeps the full line, no commission.
                result = new LineCommissionResult(
                    line.LineRef, Commissionable: false, line.CommissionBaseAmount,
                    ResolvedRate: 0m, RuleCode: null, CommissionAmount: 0m, ProviderNet: line.LineProviderRevenue);
            }
            else
            {
                // Eligible → pass the Commissionable rule dimension; InheritFromCategory → no eligibility override.
                var eligibilityDim = line.CommissionEligibility == LineCommissionEligibility.Eligible
                    ? Abstraction.Enum.CommissionEligibility.Commissionable
                    : (Abstraction.Enum.CommissionEligibility?)null;

                var ctx = new CommissionResolveContext(
                    ProviderProfileId:     providerContext.ProviderProfileId,
                    ProviderPlanId:        providerContext.ProviderPlanId,
                    CategoryCode:          line.CategoryCode,
                    ProductCode:           line.ProductCode,
                    LineType:              line.LineType,
                    CurrencyCode:          providerContext.CurrencyCode,
                    CommissionEligibility: eligibilityDim);

                // BE-P2 single-line primitive — may throw CommissionRuleConflict (propagate, do not swallow).
                var resolution = CommissionRuleResolver.Resolve(rules, ctx);

                var rate       = resolution?.Rate ?? 0m;
                var commission = MoneyMath.Round(line.CommissionBaseAmount * rate);   // rounded PER LINE
                var providerNet = line.LineProviderRevenue - commission;

                result = new LineCommissionResult(
                    line.LineRef, Commissionable: true, line.CommissionBaseAmount,
                    ResolvedRate: rate, RuleCode: resolution?.RuleCode,
                    CommissionAmount: commission, ProviderNet: providerNet);
            }

            results.Add(result);
            totalCommission  += result.CommissionAmount;      // sum of rounded line amounts
            totalProviderNet += result.ProviderNet;
            totalBase        += result.CommissionBaseAmount;
        }

        return new LineCommissionResolutionResult(
            results, totalCommission, totalProviderNet, totalBase, providerContext.CurrencyCode);
    }
}
