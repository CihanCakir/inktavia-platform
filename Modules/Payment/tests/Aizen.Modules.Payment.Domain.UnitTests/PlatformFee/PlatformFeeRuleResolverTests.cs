using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.PlatformFee;

public sealed class PlatformFeeRuleResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static PlatformFeeRuleEntity Rule(
        decimal rate = 0.025m,
        CommissionRulePriority priority = CommissionRulePriority.Standard,
        string currency = "TRY",
        string? category = null,
        string? customerType = null,
        DateTime? to = null,
        string? code = "R",
        long id = 1)
    {
        var r = PlatformFeeRuleEntity.Create(
            PlatformFeeModel.Percentage, rate, null, null, null,
            currency, category, customerType, priority, From, to, code);
        r.Id = id;
        return r;
    }

    // ── Specificity: CustomerType+Category > CustomerType > Category > Global ─────

    [Fact]
    public void Specificity_Ranks_Are_Ordered()
    {
        var custCat = Rule(id: 4, customerType: "GOLD", category: "ENG");
        var cust    = Rule(id: 3, customerType: "GOLD");
        var cat     = Rule(id: 2, category: "ENG");
        var global  = Rule(id: 1);

        PlatformFeeRuleResolver.ComputeSpecificityRank(custCat).Should().Be(4);
        PlatformFeeRuleResolver.ComputeSpecificityRank(cust).Should().Be(3);
        PlatformFeeRuleResolver.ComputeSpecificityRank(cat).Should().Be(2);
        PlatformFeeRuleResolver.ComputeSpecificityRank(global).Should().Be(1);
    }

    [Fact]
    public void Resolve_Picks_Most_Specific_CustomerType_Plus_Category()
    {
        var all = new[]
        {
            Rule(id: 1, rate: 0.030m),                                  // Global
            Rule(id: 2, rate: 0.025m, category: "ENG"),                 // Category
            Rule(id: 3, rate: 0.020m, customerType: "GOLD"),           // CustomerType
            Rule(id: 4, rate: 0.010m, customerType: "GOLD", category: "ENG"), // both
        };
        var ctx = new PlatformFeeResolveContext("TRY", "ENG", "GOLD");

        var res = PlatformFeeRuleResolver.Resolve(all, ctx);

        res!.RuleId.Should().Be(4);
        res.Rate.Should().Be(0.010m);
        res.Source.Should().Be("CustomerType+Category");
    }

    // ── Candidacy ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Currency_Mismatch_Excludes_Rule()
    {
        var eur = Rule(id: 1, currency: "EUR");
        PlatformFeeRuleResolver.IsCandidate(eur, new PlatformFeeResolveContext("TRY")).Should().BeFalse();
    }

    [Fact]
    public void Declared_Dim_Not_Supplied_Excludes_Rule()
    {
        var custScoped = Rule(id: 1, customerType: "GOLD");
        // Context supplies no CustomerType → the customer-scoped rule is excluded.
        PlatformFeeRuleResolver.IsCandidate(custScoped, new PlatformFeeResolveContext("TRY")).Should().BeFalse();
    }

    // ── Fallback ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_Falls_Back_To_Global()
    {
        var global = Rule(id: 1, rate: 0.025m);
        var catOther = Rule(id: 2, rate: 0.010m, category: "OTHER");
        var ctx = new PlatformFeeResolveContext("TRY", "ENG");

        var res = PlatformFeeRuleResolver.Resolve(new[] { global, catOther }, ctx);

        res!.RuleId.Should().Be(1);
        res.Source.Should().Be("Global");
    }

    [Fact]
    public void Resolve_Returns_Null_When_Nothing_Matches()
    {
        var catOnly = Rule(id: 1, category: "OTHER");
        var ctx = new PlatformFeeResolveContext("TRY", "ENG");
        PlatformFeeRuleResolver.Resolve(new[] { catOnly }, ctx).Should().BeNull();
    }

    // ── Priority tie-break + fail-loud conflict ──────────────────────────────────

    [Fact]
    public void Resolve_Uses_Priority_As_TieBreak()
    {
        var low  = Rule(id: 1, rate: 0.030m, priority: CommissionRulePriority.Low);
        var high = Rule(id: 2, rate: 0.010m, priority: CommissionRulePriority.High);
        var res = PlatformFeeRuleResolver.Resolve(new[] { low, high }, new PlatformFeeResolveContext("TRY"));
        res!.RuleId.Should().Be(2);
    }

    [Fact]
    public void Resolve_Throws_Conflict_On_Tie()
    {
        var a = Rule(id: 1, rate: 0.030m, priority: CommissionRulePriority.Standard);
        var b = Rule(id: 2, rate: 0.010m, priority: CommissionRulePriority.Standard);

        Action act = () => PlatformFeeRuleResolver.Resolve(new[] { a, b }, new PlatformFeeResolveContext("TRY"));

        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.PlatformFeeRuleConflict);
    }

    // ── Create/Update conflict detector ─────────────────────────────────────────

    [Fact]
    public void FindOverlappingConflict_Detects_Same_Scope_Priority_Overlap()
    {
        var existing  = Rule(id: 1, category: "ENG", priority: CommissionRulePriority.Standard);
        var candidate = Rule(id: 0, category: "ENG", priority: CommissionRulePriority.Standard);
        PlatformFeeRuleResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().NotBeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Ignores_Different_Scope()
    {
        var existing  = Rule(id: 1, category: "ENG");
        var candidate = Rule(id: 0, category: "HULL");
        PlatformFeeRuleResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().BeNull();
    }

    [Fact]
    public void FindOverlappingConflict_Excludes_Self_By_Id()
    {
        var existing  = Rule(id: 7, category: "ENG");
        var candidate = Rule(id: 7, category: "ENG");
        PlatformFeeRuleResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().BeNull();
    }
}
