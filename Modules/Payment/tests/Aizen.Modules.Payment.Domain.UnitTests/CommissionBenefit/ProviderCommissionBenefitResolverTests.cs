using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.CommissionBenefit;

public sealed class ProviderCommissionBenefitResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CommissionResolution Base(decimal rate = 0.09m, CommissionRuleType type = CommissionRuleType.Plan)
        => new(RuleId: 1, RuleCode: "CR-1", RuleType: type, Rate: rate, SpecificityRank: 30,
               Priority: CommissionRulePriority.Standard, Source: type.ToString());

    private static ProviderCommissionBenefitRuleEntity Rule(
        decimal adj = -0.01m, decimal min = 0m, decimal? maxDiscount = null, decimal? maxGmv = null,
        bool stackable = true, bool exclusive = false, long id = 1, IEnumerable<string>? categories = null)
    {
        var r = ProviderCommissionBenefitRuleEntity.Create(
            $"PCB-{id}", null, null, null, categories, adj, min, maxDiscount, maxGmv, null,
            stackable, exclusive, CommissionRulePriority.Standard, From, null, "TRY");
        r.Id = id;
        return r;
    }

    private static ProviderCommissionBenefitContext Ctx(
        decimal service = 1000m, decimal? gmvRemaining = null, decimal planFloor = 0m)
        => new(ProviderProfileId: 7, ProviderPlanId: null, CategoryCode: null,
               ServiceAmount: service, EligibleGmvRemaining: gmvRemaining, CurrencyCode: "TRY", PlanFloorRate: planFloor);

    // ── Two-stage rate (§19.5) ───────────────────────────────────────────────────

    [Fact]
    public void Base_Plus_Benefit_Gives_Effective_Rate_And_Amount()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(Base(0.09m), Ctx(1000m), new[] { Rule(adj: -0.01m) });
        res.EffectiveCommissionRate.Should().Be(0.08m);
        res.AppliedBenefitAmount.Should().Be(10m);        // 1000 × (0.09 − 0.08)
        res.AppliedBenefitRuleCodes.Should().ContainSingle();
    }

    [Fact]
    public void No_Candidate_Rules_Returns_Base()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m), Array.Empty<ProviderCommissionBenefitRuleEntity>());
        res.EffectiveCommissionRate.Should().Be(0.09m);
        res.AppliedBenefitAmount.Should().Be(0m);
    }

    // ── Floor: rule-min clamp (control 4) + plan/system floor (controls 8/9) ─────

    [Fact]
    public void RuleMinimum_Clamps_The_Effective_Rate()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(Base(0.09m), Ctx(1000m), new[] { Rule(adj: -0.02m, min: 0.08m) });
        res.EffectiveCommissionRate.Should().Be(0.08m);   // Max(0.07, 0.08)
    }

    [Fact]
    public void Below_Plan_Floor_Without_Override_Throws()
    {
        Action act = () => ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m, CommissionRuleType.Plan), Ctx(1000m, planFloor: 0.09m), new[] { Rule(adj: -0.01m) });
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBelowFloor);
    }

    [Fact]
    public void Below_Plan_Floor_With_ProviderOverride_Base_Is_Allowed()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m, CommissionRuleType.ProviderOverride), Ctx(1000m, planFloor: 0.09m), new[] { Rule(adj: -0.01m) });
        res.EffectiveCommissionRate.Should().Be(0.08m);   // admin override path permits below-floor
    }

    // ── Stackable vs Exclusive (controls 2/3) ────────────────────────────────────

    [Fact]
    public void Two_Stackable_Benefits_Sum_Adjustments()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m), new[] { Rule(adj: -0.01m, id: 1), Rule(adj: -0.01m, id: 2) });
        res.EffectiveCommissionRate.Should().Be(0.07m);   // 0.09 − 0.02
        res.AppliedBenefitRuleCodes.Should().HaveCount(2);
    }

    [Fact]
    public void Exclusive_Applies_Alone_Alongside_Others()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m),
            new[] { Rule(adj: -0.03m, stackable: false, exclusive: true, id: 1), Rule(adj: -0.01m, id: 2) });
        res.AppliedBenefitRuleCodes.Should().ContainSingle().Which.Should().Be("PCB-1");
        res.EffectiveCommissionRate.Should().Be(0.06m);   // only the exclusive −0.03
    }

    [Fact]
    public void Two_Exclusive_Benefits_Conflict()
    {
        Action act = () => ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m),
            new[] { Rule(adj: -0.01m, stackable: false, exclusive: true, id: 1), Rule(adj: -0.02m, stackable: false, exclusive: true, id: 2) });
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict);
    }

    [Fact]
    public void Non_Stackable_Multiple_Conflict()
    {
        Action act = () => ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m),
            new[] { Rule(adj: -0.01m, stackable: false, id: 1), Rule(adj: -0.02m, stackable: false, id: 2) });
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict);
    }

    // ── MaximumDiscountAmount clamp + rate recompute (control 6) ─────────────────

    [Fact]
    public void MaxDiscount_Caps_Amount_And_Recomputes_Rate()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m), new[] { Rule(adj: -0.01m, maxDiscount: 5m) });
        res.RequestedBenefitAmount.Should().Be(10m);
        res.AppliedBenefitAmount.Should().Be(5m);          // capped
        res.EffectiveCommissionRate.Should().Be(0.085m);   // 0.09 − 5/1000
        res.AdjustmentReason.Should().Contain("capped");
    }

    // ── MaximumEligibleGMV split (control 5) ─────────────────────────────────────

    [Fact]
    public void GMV_Cap_Splits_Benefited_And_NonBenefited()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m, gmvRemaining: 400m), new[] { Rule(adj: -0.01m) });
        res.BenefitedServiceAmount.Should().Be(400m);
        res.NonBenefitedServiceAmount.Should().Be(600m);
        res.AppliedBenefitAmount.Should().Be(4m);          // 400 × 0.01, benefit only on eligible portion
    }

    [Fact]
    public void Zero_Eligible_GMV_Applies_No_Benefit()
    {
        var res = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(
            Base(0.09m), Ctx(1000m, gmvRemaining: 0m), new[] { Rule(adj: -0.01m) });
        res.AppliedBenefitAmount.Should().Be(0m);
        res.EffectiveCommissionRate.Should().Be(0.09m);
    }

    // ── Purity / determinism ─────────────────────────────────────────────────────

    [Fact]
    public void Same_Inputs_Yield_Identical_Resolution()
    {
        var rules = new[] { Rule(adj: -0.01m, id: 1) };
        var a = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(Base(0.09m), Ctx(1000m), rules);
        var b = ProviderCommissionBenefitResolver.ResolveEffectiveCommission(Base(0.09m), Ctx(1000m), rules);
        a.Should().BeEquivalentTo(b);   // deep structural equality (record has List members)
    }
}
