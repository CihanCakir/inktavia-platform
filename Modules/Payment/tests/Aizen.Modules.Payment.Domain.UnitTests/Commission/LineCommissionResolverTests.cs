using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Commission;

public sealed class LineCommissionResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CommissionRuleEntity PlanRule(long planId, decimal rate, long id = 1)
    {
        var r = CommissionRuleEntity.CreateForPlan(planId, rate, From, null, CommissionRulePriority.Standard, null, $"CR-P{id}");
        r.Id = id;
        return r;
    }

    private static CommissionRuleEntity CategoryRule(string code, decimal rate, long id = 2)
    {
        var r = CommissionRuleEntity.CreateForCategory(code, rate, From, null, CommissionRulePriority.Standard, null, $"CR-C{id}");
        r.Id = id;
        return r;
    }

    private static CommissionRuleEntity GlobalRule(decimal rate, CommissionRulePriority p = CommissionRulePriority.Standard, long id = 3)
    {
        var r = CommissionRuleEntity.CreateGlobal(rate, From, null, p, null, $"CR-G{id}");
        r.Id = id;
        return r;
    }

    private static LineCommissionInput Line(
        string reff, LineCommissionEligibility elig, decimal baseAmt, decimal revenue,
        LineType? lineType = null, string? category = null)
        => new(reff, lineType, null, elig, category, baseAmt, revenue);

    // ── Smoke: {Service 5000 @STANDARD 0.12, Travel 800 exempt} ─────────────────

    [Fact]
    public void Service_Eligible_Plus_Travel_Exempt_Aggregates_Correctly()
    {
        var rules = new[] { PlanRule(2, 0.12m) };
        var ctx = new LineCommissionProviderContext(7, ProviderPlanId: 2, "TRY");
        var lines = new[]
        {
            Line("svc", LineCommissionEligibility.Eligible, 5000m, 5000m, LineType.Labor, "ENG"),
            Line("trv", LineCommissionEligibility.Exempt,   0m,    800m,  LineType.Travel),
        };

        var res = LineCommissionResolver.Resolve(rules, ctx, lines);

        var svc = res.Lines.First(l => l.LineRef == "svc");
        svc.Commissionable.Should().BeTrue();
        svc.ResolvedRate.Should().Be(0.12m);
        svc.CommissionAmount.Should().Be(600m);      // Round(5000 × 0.12)
        svc.ProviderNet.Should().Be(4400m);          // 5000 − 600

        var trv = res.Lines.First(l => l.LineRef == "trv");
        trv.Commissionable.Should().BeFalse();
        trv.ResolvedRate.Should().Be(0m);
        trv.CommissionAmount.Should().Be(0m);
        trv.ProviderNet.Should().Be(800m);           // provider keeps the full exempt line

        res.TransactionCommission.Should().Be(600m);
        res.TransactionProviderNet.Should().Be(5200m);   // 4400 + 800
        res.TransactionCommissionBase.Should().Be(5000m);
    }

    // ── InheritFromCategory resolves by category/global (no eligibility override) ─

    [Fact]
    public void Inherit_Resolves_By_Category()
    {
        var rules = new[] { GlobalRule(0.15m), CategoryRule("ENG", 0.10m) };
        var ctx = new LineCommissionProviderContext(7, ProviderPlanId: null, "TRY");
        var lines = new[] { Line("p", LineCommissionEligibility.InheritFromCategory, 1000m, 1000m, LineType.Part, "ENG") };

        var res = LineCommissionResolver.Resolve(rules, ctx, lines);

        res.Lines.Single().ResolvedRate.Should().Be(0.10m);     // category beats global
        res.Lines.Single().CommissionAmount.Should().Be(100m);
    }

    // ── Aggregation: Σ line = transaction (per-line rounding preserved) ──────────

    [Fact]
    public void Transaction_Totals_Are_Sum_Of_Per_Line_Rounded_Amounts()
    {
        var rules = new[] { GlobalRule(0.10m) };
        var ctx = new LineCommissionProviderContext(7, null, "TRY");
        var lines = new[]
        {
            Line("a", LineCommissionEligibility.Eligible, 100.05m, 100.05m),  // 100.05×0.10 = 10.005 → 10.01
            Line("b", LineCommissionEligibility.Eligible, 100.05m, 100.05m),  // 10.01
        };

        var res = LineCommissionResolver.Resolve(rules, ctx, lines);

        res.Lines[0].CommissionAmount.Should().Be(10.01m);           // away-from-zero per line
        res.TransactionCommission.Should().Be(20.02m);               // sum of rounded (not Round(200.10×0.10)=20.01)
        res.TransactionCommission.Should().Be(res.Lines.Sum(l => l.CommissionAmount));
    }

    // ── Conflict propagation ─────────────────────────────────────────────────────

    [Fact]
    public void Line_Conflict_Propagates_CommissionRuleConflict()
    {
        var rules = new[] { GlobalRule(0.10m, id: 1), GlobalRule(0.12m, id: 2) };   // two globals tie
        var ctx = new LineCommissionProviderContext(7, null, "TRY");
        var lines = new[] { Line("x", LineCommissionEligibility.Eligible, 1000m, 1000m) };

        Action act = () => LineCommissionResolver.Resolve(rules, ctx, lines);

        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.CommissionRuleConflict);
    }

    // ── Purity / determinism ─────────────────────────────────────────────────────

    [Fact]
    public void Same_Inputs_Yield_Equivalent_Result()
    {
        var rules = new[] { PlanRule(2, 0.12m) };
        var ctx = new LineCommissionProviderContext(7, 2, "TRY");
        var lines = new[] { Line("svc", LineCommissionEligibility.Eligible, 5000m, 5000m) };

        var a = LineCommissionResolver.Resolve(rules, ctx, lines);
        var b = LineCommissionResolver.Resolve(rules, ctx, lines);
        a.Should().BeEquivalentTo(b);
    }

    // ── Typed remote-call DTO round-trip (no object) ────────────────────────────

    [Fact]
    public void RemoteCall_Request_Round_Trips_Typed()
    {
        var request = new ResolveLineCommissionsRemoteCallRequest
        {
            ProviderProfileId = 7,
            ProviderPlanId    = 2,
            CurrencyCode      = "TRY",
            Lines = new()
            {
                new ResolveLineCommissionInputDto
                {
                    LineRef = "svc", LineType = LineType.Labor, CommissionEligibility = LineCommissionEligibility.Eligible,
                    CategoryCode = "ENG", CommissionBaseAmount = 5000m, LineProviderRevenue = 5000m,
                },
            },
        };

        var json = JsonSerializer.Serialize(request);
        var round = JsonSerializer.Deserialize<ResolveLineCommissionsRemoteCallRequest>(json)!;

        round.ProviderProfileId.Should().Be(7);
        round.ProviderPlanId.Should().Be(2);
        round.Lines.Should().ContainSingle();
        round.Lines[0].LineType.Should().Be(LineType.Labor);
        round.Lines[0].CommissionEligibility.Should().Be(LineCommissionEligibility.Eligible);
        round.Lines[0].CommissionBaseAmount.Should().Be(5000m);
    }
}
