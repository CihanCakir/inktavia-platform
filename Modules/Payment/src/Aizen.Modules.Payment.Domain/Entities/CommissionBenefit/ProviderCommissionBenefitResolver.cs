using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

/// <summary>Stage-2 resolve context. <see cref="EligibleGmvRemaining"/> null = no GMV cap; <see cref="PlanFloorRate"/>
/// is the plan/system minimum (e.g. 0.09 for PREMIUM_PARTNER, 0 otherwise — control 8).</summary>
public sealed record ProviderCommissionBenefitContext(
    long     ProviderProfileId,
    long?    ProviderPlanId,
    string?  CategoryCode,
    decimal  ServiceAmount,
    decimal? EligibleGmvRemaining = null,
    string   CurrencyCode = "TRY",
    decimal  PlanFloorRate = 0m);

/// <summary>
/// The stage-2 result (§4.8). <see cref="AppliedBenefitAmount"/> is the P5-engine input
/// <c>ProviderCommissionBenefitCost</c>; <see cref="EffectiveCommissionRate"/> is what P8 applies.
/// </summary>
public sealed record EffectiveCommissionResolution(
    string       BaseRuleCode,
    decimal      BaseRate,
    List<string> AppliedBenefitRuleCodes,
    decimal      RequestedAdjustment,
    decimal      AppliedAdjustment,
    decimal      EffectiveCommissionRate,
    decimal      RequestedBenefitAmount,
    decimal      AppliedBenefitAmount,
    decimal      BenefitedServiceAmount,
    decimal      NonBenefitedServiceAmount,
    string?      AdjustmentReason,
    List<long>   AppliedBenefitRuleIds);

/// <summary>
/// Pure two-stage commission resolver (§19.4/§19.5). Layers provider commission BENEFITS on top of the BE-P2 base rate —
/// it never recomputes the base. Enforces the 9 binding controls, incl. boost decoupling (control 1) BY CONSTRUCTION:
/// this type only reads <see cref="ProviderCommissionBenefitRuleEntity"/>, never premium/boost products. No writes.
/// </summary>
public static class ProviderCommissionBenefitResolver
{
    public static EffectiveCommissionResolution ResolveEffectiveCommission(
        CommissionResolution baseResolution,
        ProviderCommissionBenefitContext ctx,
        IEnumerable<ProviderCommissionBenefitRuleEntity> candidateRules)
    {
        var baseRate = baseResolution.Rate;

        // ── Candidates: currency + scope match. The repository pre-filters active/effective-at-atUtc, so the
        //    resolver stays pure/deterministic (no DateTime.UtcNow).
        var candidates = candidateRules
            .Where(r => string.Equals(r.CurrencyCode, ctx.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                     && r.MatchesScope(ctx.ProviderProfileId, ctx.ProviderPlanId, ctx.CategoryCode))
            .ToList();

        // ── Combination controls (§19.5 controls 1/2/3) → the set that actually applies ──
        var applied = SelectApplied(candidates);

        // ── GMV split (control 5) — benefit only within eligible GMV ──
        var benefited    = ctx.EligibleGmvRemaining is { } rem ? Min(ctx.ServiceAmount, Max(0m, rem)) : ctx.ServiceAmount;
        var nonBenefited = ctx.ServiceAmount - benefited;

        if (applied.Count == 0 || benefited <= 0m)
            return NoBenefit(baseResolution, ctx, benefited, nonBenefited,
                applied.Count == 0 ? null : "Benefit not applied: no eligible GMV remaining.");

        // ── Adjustment sum + rule-level floor + tightest monetary cap ──
        var requestedAdjustment = applied.Sum(r => r.AdjustmentPercentagePoints);   // ≤ 0
        var ruleMin             = applied.Max(r => r.MinimumCommissionRate);         // control 4
        var maxDiscount         = applied.Where(r => r.MaximumDiscountAmount.HasValue)
                                         .Select(r => r.MaximumDiscountAmount!.Value)
                                         .DefaultIfEmpty(decimal.MaxValue).Min();

        // ── Effective rate (stage 2): Max(Base + ΣAdj, ruleMin) ──
        var targetEffective = MoneyMath.RoundRate(Max(baseRate + requestedAdjustment, ruleMin));

        // ── Plan/system floor (controls 8/9) ──
        if (targetEffective < ctx.PlanFloorRate
            && baseResolution.RuleType != CommissionRuleType.ProviderOverride)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderCommissionBelowFloor,
                $"Effective commission {targetEffective} would fall below the plan floor {ctx.PlanFloorRate} without " +
                "an explicit admin ProviderOverride base rule (§19.5-9). Raise the benefit's MinimumCommissionRate.");

        // ── Monetary benefit + MaximumDiscountAmount clamp (control 6, recompute rate) ──
        var requestedBenefit = Max(0m, MoneyMath.Round(benefited * (baseRate - targetEffective)));
        var appliedBenefit   = requestedBenefit;
        var effectiveRate    = targetEffective;

        if (appliedBenefit > maxDiscount)
        {
            appliedBenefit = maxDiscount;
            // Recompute the effective rate the capped amount implies, so rate and amount stay consistent.
            effectiveRate = MoneyMath.RoundRate(baseRate - (appliedBenefit / benefited));
        }

        var appliedAdjustment = MoneyMath.RoundRate(effectiveRate - baseRate);   // ≤ 0
        var reason = appliedBenefit != requestedBenefit
            ? $"Benefit capped to MaximumDiscountAmount ({maxDiscount}); effective rate recomputed."
            : (nonBenefited > 0m ? "Benefit applied only within the eligible GMV." : null);

        return new EffectiveCommissionResolution(
            BaseRuleCode:              baseResolution.RuleCode,
            BaseRate:                  baseRate,
            AppliedBenefitRuleCodes:   applied.Select(r => r.RuleCode ?? string.Empty).ToList(),
            RequestedAdjustment:       MoneyMath.RoundRate(requestedAdjustment),
            AppliedAdjustment:         appliedAdjustment,
            EffectiveCommissionRate:   effectiveRate,
            RequestedBenefitAmount:    requestedBenefit,
            AppliedBenefitAmount:      appliedBenefit,
            BenefitedServiceAmount:    benefited,
            NonBenefitedServiceAmount: nonBenefited,
            AdjustmentReason:          reason,
            AppliedBenefitRuleIds:     applied.Select(r => r.Id).ToList());
    }

    // ── Combination controls (§19.5 1/2/3) ──────────────────────────────────────

    private static List<ProviderCommissionBenefitRuleEntity> SelectApplied(
        List<ProviderCommissionBenefitRuleEntity> candidates)
    {
        if (candidates.Count == 0) return candidates;

        var exclusives = candidates.Where(r => r.Exclusive).ToList();
        if (exclusives.Count > 1)
            throw Conflict(exclusives, "multiple Exclusive benefits match");
        if (exclusives.Count == 1)
            return exclusives;   // control 3: the exclusive one applies ALONE

        if (candidates.Count == 1)
            return candidates;

        // No exclusive, multiple candidates: they combine ONLY if all stackable (control 2).
        if (candidates.All(r => r.Stackable))
            return candidates;

        throw Conflict(candidates, "multiple non-stackable benefits match and none is a clear winner");
    }

    private static AizenBusinessException Conflict(IEnumerable<ProviderCommissionBenefitRuleEntity> rules, string why)
        => new((int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict,
               $"Provider commission benefit conflict: {why} (ids: [{string.Join(", ", rules.Select(r => r.Id))}]).");

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static EffectiveCommissionResolution NoBenefit(
        CommissionResolution b, ProviderCommissionBenefitContext ctx, decimal benefited, decimal nonBenefited, string? reason)
        => new(b.RuleCode, b.Rate, new(), 0m, 0m, b.Rate, 0m, 0m, benefited, nonBenefited, reason, new());

    private static decimal Min(decimal a, decimal b) => a <= b ? a : b;
    private static decimal Max(decimal a, decimal b) => a >= b ? a : b;
}
