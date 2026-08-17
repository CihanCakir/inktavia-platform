using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.CustomerDiscount;

public sealed class CustomerDiscountRuleTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CustomerDiscountRuleEntity Rule(
        long? plan = null, string? category = null, decimal rate = 0.10m,
        CommissionRulePriority priority = CommissionRulePriority.Standard,
        CustomerDiscountFundingMode funding = CustomerDiscountFundingMode.PlatformFunded,
        decimal? platformRate = null, decimal? providerRate = null, bool requiresConsent = false,
        decimal? min = null, decimal? max = null, long id = 1)
    {
        var r = CustomerDiscountRuleEntity.Create(
            plan, category, "TRY", CustomerDiscountType.Percent, rate, null, min, max,
            funding, platformRate, providerRate, requiresConsent, priority, From, null, $"CDR-{id}");
        r.Id = id;
        return r;
    }

    // ── Specificity: Plan+Category > Plan > Category > Global ─────────────────────

    [Fact]
    public void Specificity_Ranks_Are_Ordered()
    {
        CustomerDiscountRuleResolver.ComputeSpecificityRank(Rule(plan: 5, category: "ENG")).Should().Be(4);
        CustomerDiscountRuleResolver.ComputeSpecificityRank(Rule(plan: 5)).Should().Be(3);
        CustomerDiscountRuleResolver.ComputeSpecificityRank(Rule(category: "ENG")).Should().Be(2);
        CustomerDiscountRuleResolver.ComputeSpecificityRank(Rule()).Should().Be(1);
    }

    [Fact]
    public void Resolve_Picks_Most_Specific()
    {
        var rules = new[]
        {
            Rule(id: 1, rate: 0.05m),                          // Global
            Rule(id: 2, category: "ENG", rate: 0.08m),         // Category
            Rule(id: 3, plan: 5, rate: 0.10m),                 // Plan
            Rule(id: 4, plan: 5, category: "ENG", rate: 0.15m),// Plan+Category
        };
        var ctx = new CustomerDiscountResolveContext(5, "ENG", "TRY");
        CustomerDiscountRuleResolver.Resolve(rules, ctx)!.Id.Should().Be(4);
    }

    [Fact]
    public void Resolve_Throws_Conflict_On_Tie()
    {
        var a = Rule(id: 1, plan: 5, rate: 0.10m, priority: CommissionRulePriority.Standard);
        var b = Rule(id: 2, plan: 5, rate: 0.12m, priority: CommissionRulePriority.Standard);
        Action act = () => CustomerDiscountRuleResolver.Resolve(new[] { a, b }, new CustomerDiscountResolveContext(5, null, "TRY"));
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerDiscountRuleConflict);
    }

    [Fact]
    public void Rule_For_Other_Plan_Is_Not_A_Candidate()
        => CustomerDiscountRuleResolver.IsCandidate(Rule(plan: 5), new CustomerDiscountResolveContext(9, null, "TRY"))
            .Should().BeFalse();

    // ── Requested-discount computation: Min inert, Max clamp ─────────────────────

    [Fact]
    public void ComputeRequestedDiscount_Percent_Rounds()
        => Rule(rate: 0.10m).ComputeRequestedDiscount(1000m).Should().Be(100m);

    [Fact]
    public void MinimumPurchase_Makes_Rule_Inert_Below_Threshold()
        => Rule(rate: 0.10m, min: 500m).ComputeRequestedDiscount(400m).Should().Be(0m);

    [Fact]
    public void MaximumDiscount_Clamps()
        => Rule(rate: 0.10m, max: 50m).ComputeRequestedDiscount(1000m).Should().Be(50m);   // 100 clamped to 50

    // ── Funding coherence: Shared rates must sum to 100%; funding source mandatory

    [Fact]
    public void Shared_Rates_Not_Summing_To_One_Throws()
    {
        Action act = () => CustomerDiscountRuleEntity.Create(
            null, null, "TRY", CustomerDiscountType.Percent, 0.10m, null, null, null,
            CustomerDiscountFundingMode.Shared, 0.6m, 0.3m, true, CommissionRulePriority.Standard, From, null, "CDR");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerDiscountRuleInvalid);
    }

    [Fact]
    public void Undefined_Funding_Source_Throws()
    {
        Action act = () => CustomerDiscountRuleEntity.Create(
            null, null, "TRY", CustomerDiscountType.Percent, 0.10m, null, null, null,
            (CustomerDiscountFundingMode)99, null, null, false, CommissionRulePriority.Standard, From, null, "CDR");
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CustomerDiscountRuleInvalid);
    }

    // ── Funding allocation (§19.6) ───────────────────────────────────────────────

    [Fact]
    public void PlatformFunded_Allocates_All_To_Platform()
    {
        var a = CustomerDiscountFundingCalculator.Allocate(100m, Rule(funding: CustomerDiscountFundingMode.PlatformFunded), providerConsent: false);
        a.PlatformFundedAmount.Should().Be(100m);
        a.ProviderFundedAmount.Should().Be(0m);
        a.AppliedDiscountAmount.Should().Be(100m);
    }

    [Fact]
    public void ProviderFunded_With_Consent_Allocates_To_Provider()
    {
        var a = CustomerDiscountFundingCalculator.Allocate(100m, Rule(funding: CustomerDiscountFundingMode.ProviderFunded, requiresConsent: true), providerConsent: true);
        a.ProviderFundedAmount.Should().Be(100m);
        a.AppliedDiscountAmount.Should().Be(100m);
    }

    [Fact]
    public void ProviderFunded_Without_Consent_Is_Not_Applied_And_Not_Platform_Funded()
    {
        var a = CustomerDiscountFundingCalculator.Allocate(100m, Rule(funding: CustomerDiscountFundingMode.ProviderFunded, requiresConsent: true), providerConsent: false);
        a.PlatformFundedAmount.Should().Be(0m, "provider discount must never be silently platform-funded");
        a.ProviderFundedAmount.Should().Be(0m);
        a.AppliedDiscountAmount.Should().Be(0m);
        a.UnappliedDueToConsent.Should().Be(100m);
    }

    [Fact]
    public void Shared_Splits_By_Funding_Rates()
    {
        var rule = Rule(funding: CustomerDiscountFundingMode.Shared, platformRate: 0.6m, providerRate: 0.4m, requiresConsent: true);
        var a = CustomerDiscountFundingCalculator.Allocate(100m, rule, providerConsent: true);
        a.PlatformFundedAmount.Should().Be(60m);
        a.ProviderFundedAmount.Should().Be(40m);
        a.AppliedDiscountAmount.Should().Be(100m);
    }

    [Fact]
    public void Shared_Without_Consent_Applies_Only_Platform_Portion()
    {
        var rule = Rule(funding: CustomerDiscountFundingMode.Shared, platformRate: 0.6m, providerRate: 0.4m, requiresConsent: true);
        var a = CustomerDiscountFundingCalculator.Allocate(100m, rule, providerConsent: false);
        a.PlatformFundedAmount.Should().Be(60m);
        a.ProviderFundedAmount.Should().Be(0m);
        a.AppliedDiscountAmount.Should().Be(60m);
        a.UnappliedDueToConsent.Should().Be(40m);
    }
}
