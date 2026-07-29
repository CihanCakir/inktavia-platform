using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Commission;

public sealed class CommissionRuleResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Flexible builder: picks a factory by the strongest primary dim, then augments ──
    private static CommissionRuleEntity Build(
        decimal                 rate       = 0.10m,
        CommissionRulePriority  priority   = CommissionRulePriority.Standard,
        long?                   provider   = null,
        long?                   plan       = null,
        string?                 category   = null,
        string?                 product    = null,
        LineType?               lineType   = null,
        TransactionContextType? context    = null,
        CommercialModel?        commercial = null,
        SalesChannel?           channel    = null,
        string?                 currency   = null,
        CommissionEligibility?  elig       = null,
        DateTime?               to         = null,
        string?                 code       = "R",
        long                    id         = 1)
    {
        CommissionRuleEntity r =
              provider.HasValue    ? CommissionRuleEntity.CreateProviderOverride(provider.Value, rate, From, to, priority, null, code)
            : plan.HasValue        ? CommissionRuleEntity.CreateForPlan(plan.Value, rate, From, to, priority, null, code)
            : category is not null  ? CommissionRuleEntity.CreateForCategory(category, rate, From, to, priority, null, code)
            :                        CommissionRuleEntity.CreateGlobal(rate, From, to, priority, null, code);

        r.SetPrimaryScope(provider, plan, category);
        if (context.HasValue || product is not null || channel.HasValue) r.SetContextDimensions(context, product, channel);
        if (lineType.HasValue || elig.HasValue) r.SetLineDimensions(lineType, elig);
        if (commercial.HasValue || currency is not null) r.SetMetadata(null, currency, commercial);
        r.Id = id;
        return r;
    }

    // ── 8-level specificity: each level, given a full context, wins over all broader levels ──

    [Fact]
    public void Specificity_EightLevels_Rank_Is_Ordered()
    {
        var providerPlanCategory = Build(id: 8, provider: 5, plan: 3, category: "ENG");
        var providerCategory     = Build(id: 7, provider: 5, category: "ENG");
        var providerPlan         = Build(id: 6, provider: 5, plan: 3);
        var provider             = Build(id: 5, provider: 5);
        var planCategory         = Build(id: 4, plan: 3, category: "ENG");
        var plan                 = Build(id: 3, plan: 3);
        var category             = Build(id: 2, category: "ENG");
        var global               = Build(id: 1);

        CommissionRuleResolver.ComputeSpecificityRank(providerPlanCategory).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(providerCategory));
        CommissionRuleResolver.ComputeSpecificityRank(providerCategory).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(providerPlan));
        CommissionRuleResolver.ComputeSpecificityRank(providerPlan).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(provider));
        CommissionRuleResolver.ComputeSpecificityRank(provider).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(planCategory));
        CommissionRuleResolver.ComputeSpecificityRank(planCategory).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(plan));
        CommissionRuleResolver.ComputeSpecificityRank(plan).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(category));
        CommissionRuleResolver.ComputeSpecificityRank(category).Should().BeGreaterThan(
            CommissionRuleResolver.ComputeSpecificityRank(global));
    }

    [Fact]
    public void Resolve_Picks_The_Most_Specific_Candidate()
    {
        var all = new[]
        {
            Build(id: 1, rate: 0.20m),                                            // Global
            Build(id: 3, rate: 0.15m, plan: 3),                                   // Plan
            Build(id: 8, rate: 0.05m, provider: 5, plan: 3, category: "ENG"),     // Provider+Plan+Category
            Build(id: 5, rate: 0.10m, provider: 5),                               // Provider
        };
        var ctx = new CommissionResolveContext(ProviderProfileId: 5, ProviderPlanId: 3, CategoryCode: "ENG");

        var res = CommissionRuleResolver.Resolve(all, ctx);

        res.Should().NotBeNull();
        res!.RuleId.Should().Be(8);
        res.Rate.Should().Be(0.05m);
    }

    [Fact]
    public void Resolve_FinerDim_Wins_Over_Broader_SameLevel()
    {
        var plain      = Build(id: 3, rate: 0.15m, plan: 3);
        var withProduct = Build(id: 30, rate: 0.09m, plan: 3, product: "ENGINE_X");
        var ctx = new CommissionResolveContext(ProviderPlanId: 3, ProductCode: "ENGINE_X");

        var res = CommissionRuleResolver.Resolve(new[] { plain, withProduct }, ctx);

        res!.RuleId.Should().Be(30);   // finer ProductCode dim beats the plain plan rule at the same base level
        res.Rate.Should().Be(0.09m);
    }

    // ── Candidacy: a declared finer dim not supplied by the context excludes the rule ──

    [Fact]
    public void Rule_With_Unmatched_Declared_Dim_Is_Not_A_Candidate()
    {
        var productScoped = Build(id: 30, plan: 3, product: "ENGINE_X");
        // Context supplies the plan but NOT the product → the product-scoped rule must be excluded.
        var ctx = new CommissionResolveContext(ProviderPlanId: 3);

        CommissionRuleResolver.IsCandidate(productScoped, ctx).Should().BeFalse();
    }

    [Fact]
    public void CargoDry_ContextType_Rule_Excluded_From_General_Resolve()
    {
        var cargoRule = Build(id: 5, provider: 5, context: TransactionContextType.CargoDry);
        var ctx = new CommissionResolveContext(ProviderProfileId: 5); // no ContextType supplied

        CommissionRuleResolver.IsCandidate(cargoRule, ctx).Should().BeFalse();
    }

    // ── Fallback ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_Falls_Back_To_Global_When_No_Specific_Match()
    {
        var global = Build(id: 1, rate: 0.15m);
        var planRule = Build(id: 3, rate: 0.12m, plan: 99); // different plan
        var ctx = new CommissionResolveContext(ProviderPlanId: 3);

        var res = CommissionRuleResolver.Resolve(new[] { global, planRule }, ctx);

        res!.RuleId.Should().Be(1);
        res.Source.Should().Be(CommissionRuleType.Global.ToString());
    }

    [Fact]
    public void Resolve_Returns_Null_When_Nothing_Matches()
    {
        var planRule = Build(id: 3, plan: 99);
        var ctx = new CommissionResolveContext(ProviderPlanId: 3);

        CommissionRuleResolver.Resolve(new[] { planRule }, ctx).Should().BeNull();
    }

    // ── Priority tie-break + fail-loud conflict ──────────────────────────────────

    [Fact]
    public void Resolve_Uses_Priority_As_TieBreak_Within_Same_Specificity()
    {
        var low  = Build(id: 1, rate: 0.20m, priority: CommissionRulePriority.Low);
        var high = Build(id: 2, rate: 0.10m, priority: CommissionRulePriority.High);
        var ctx = new CommissionResolveContext();

        var res = CommissionRuleResolver.Resolve(new[] { low, high }, ctx);

        res!.RuleId.Should().Be(2);            // High priority wins the tie on specificity
        res.Priority.Should().Be(CommissionRulePriority.High);
    }

    [Fact]
    public void Resolve_Throws_Conflict_On_Tie_Of_Specificity_And_Priority()
    {
        var a = Build(id: 1, rate: 0.20m, priority: CommissionRulePriority.Standard);
        var b = Build(id: 2, rate: 0.10m, priority: CommissionRulePriority.Standard);
        var ctx = new CommissionResolveContext();

        Action act = () => CommissionRuleResolver.Resolve(new[] { a, b }, ctx);

        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CommissionRuleConflict);
    }

    // ── Create/Update conflict detector (§3) ─────────────────────────────────────

    [Fact]
    public void FindOverlappingConflict_Detects_Same_Scope_Priority_Overlapping_Window()
    {
        var existing  = Build(id: 1, plan: 3, priority: CommissionRulePriority.Standard);
        var candidate = Build(id: 0, plan: 3, priority: CommissionRulePriority.Standard);

        CommissionRuleResolver.FindOverlappingConflict(candidate, new[] { existing })
            .Should().NotBeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Ignores_Different_Priority()
    {
        var existing  = Build(id: 1, plan: 3, priority: CommissionRulePriority.Standard);
        var candidate = Build(id: 0, plan: 3, priority: CommissionRulePriority.Low);

        CommissionRuleResolver.FindOverlappingConflict(candidate, new[] { existing })
            .Should().BeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Ignores_NonOverlapping_Windows()
    {
        var existing  = Build(id: 1, plan: 3, to: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        // candidate starts in 2022 → no overlap with [2020,2021)
        var candidate = CommissionRuleEntity.CreateForPlan(
            3, 0.12m, new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), null,
            CommissionRulePriority.Standard, null, "R2");
        candidate.Id = 0;

        CommissionRuleResolver.FindOverlappingConflict(candidate, new[] { existing })
            .Should().BeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Excludes_Self_By_Id()
    {
        var existing = Build(id: 7, plan: 3, priority: CommissionRulePriority.Standard);
        // Same identity (Id 7) → an update must not conflict with itself.
        var candidate = Build(id: 7, plan: 3, priority: CommissionRulePriority.Standard);

        CommissionRuleResolver.FindOverlappingConflict(candidate, new[] { existing })
            .Should().BeNull();
    }
}
