using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.ProfitProtection;

/// <summary>
/// BE-S9 (§20.12) — the pure LINE-level profit-protection engine. Each line must independently clear its own floor
/// BEFORE the transaction gates; a line's loss can NOT be hidden in another line's profit (NO netting). Every threshold
/// comes from the policy (non-part) or the S5 allowance (part) — no hardcoded constant.
/// </summary>
public sealed class LineProfitProtectionEngineTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // A policy with explicit line-level knobs (transaction-level fields are irrelevant to the line engine → 0).
    private static ProfitProtectionPolicyEntity Policy(
        decimal minRecvRate = 0m, decimal minRecvAmt = 0m,
        decimal allowedProvRate = 1m, decimal allowedPlatRate = 1m,
        decimal commFloorRate = 0m, decimal minContribRate = 0m,
        bool lossEnabled = false, decimal lossMaxDeficit = 0m)
    {
        var p = ProfitProtectionPolicyEntity.Create(
            "TRY", 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
            From, null, "PPOL-1", null, null,
            minRecvRate, minRecvAmt, allowedProvRate, allowedPlatRate,
            commFloorRate, minContribRate, lossEnabled, lossMaxDeficit);
        p.Id = 1;
        return p;
    }

    private static LineProfitProtectionLineInput Line(
        string @ref, bool part = false, decimal providerNet = 1000m,
        decimal provFunded = 0m, decimal platFunded = 0m, decimal resolvedRate = 0.12m,
        decimal commissionNet = 120m, decimal commissionBase = 1000m, decimal lineBase = 1000m)
        => new(@ref, part, providerNet, provFunded, platFunded, resolvedRate, commissionNet, commissionBase, lineBase);

    private static LineProfitProtectionEvaluation Eval(
        IReadOnlyList<LineProfitProtectionLineInput> lines, ProfitProtectionPolicyEntity? policy,
        IReadOnlyDictionary<string, LinePartAllowance>? allowances = null,
        decimal feeNet = 0m, decimal benefit = 0m)
        => LineProfitProtectionEngine.Evaluate(
            lines, policy, allowances ?? new Dictionary<string, LinePartAllowance>(), feeNet, benefit);

    // ── Baseline: all lines clear → Approved ────────────────────────────────────
    [Fact]
    public void AllLinesClear_Approved()
    {
        var r = Eval(new[] { Line("A"), Line("B") }, Policy());
        r.Passed.Should().BeTrue();
        r.State.Should().Be(ProfitProtectionDecisionState.Approved);
        r.Lines.Should().OnlyContain(l => l.Passed);
    }

    // ── (1) Provider min-receivable ─────────────────────────────────────────────
    [Fact]
    public void ProviderNetBelowMinReceivable_Rejected()
    {
        var r = Eval(new[] { Line("A", providerNet: 90m) }, Policy(minRecvAmt: 100m));
        r.Passed.Should().BeFalse();
        r.State.Should().Be(ProfitProtectionDecisionState.Rejected);
        r.PrimaryErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionProviderReceivableBelowFloor);
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.ProviderReceivableBelowFloor);
    }

    // ── (2) Funded-discount caps ────────────────────────────────────────────────
    [Fact]
    public void ProviderFundedDiscountOverCap_Rejected()
    {
        // allowed provider-funded = lineBase(1000) × 0.10 = 100; line has 150.
        var r = Eval(new[] { Line("A", provFunded: 150m) }, Policy(allowedProvRate: 0.10m));
        r.Passed.Should().BeFalse();
        r.PrimaryErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionFundedDiscountExceedsCap);
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.FundedDiscountExceedsCap);
    }

    [Fact]
    public void PlatformFundedDiscountOverCap_Rejected()
    {
        var r = Eval(new[] { Line("A", platFunded: 300m, commissionNet: 400m) }, Policy(allowedPlatRate: 0.10m));
        r.Passed.Should().BeFalse();
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.FundedDiscountExceedsCap);
    }

    // ── (3) Commission floor — reuses the P7 ProviderCommissionBelowFloor code ───
    [Fact]
    public void CommissionRateBelowFloor_Rejected_ReusesP7Code()
    {
        var r = Eval(new[] { Line("A", resolvedRate: 0.05m) }, Policy(commFloorRate: 0.10m));
        r.Passed.Should().BeFalse();
        r.PrimaryErrorCode.Should().Be((int)PaymentErrorCode.ProviderCommissionBelowFloor);   // 5078 reused, not duplicated
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.CommissionBelowFloor);
    }

    // ── (4) Negative contribution (ban) ─────────────────────────────────────────
    [Fact]
    public void NegativeLineContribution_Rejected()
    {
        // contribution = commissionNet(10) − platFunded(100) = −90 < 0
        var r = Eval(new[] { Line("A", commissionNet: 10m, platFunded: 100m) }, Policy());
        r.Passed.Should().BeFalse();
        r.PrimaryErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionNegativeContribution);
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.NegativeContribution);
        r.Lines.Single().LinePlatformContribution.Should().Be(-90m);
    }

    // ── (5) THE NETTING CASE — a profitable line can NOT rescue a loss line ──────
    [Fact]
    public void Netting_ProfitableLinePlusLossLine_StillRejected()
    {
        var profit = Line("A", commissionNet: 600m, platFunded: 0m);   // +600
        var loss   = Line("B", commissionNet: 10m,  platFunded: 100m); // −90
        var r = Eval(new[] { profit, loss }, Policy());

        r.Passed.Should().BeFalse();                                    // no netting: the +600 does NOT offset the −90
        r.State.Should().Be(ProfitProtectionDecisionState.Rejected);
        r.Lines.Single(l => l.LineRef == "A").Passed.Should().BeTrue(); // the profitable line is fine on its own
        r.Lines.Single(l => l.LineRef == "B").Passed.Should().BeFalse();
        r.Lines.Single(l => l.LineRef == "B").Breach.Should().Be(LineProfitProtectionBreach.NegativeContribution);
    }

    // ── (6) Strategic-loss exception: on+within limit allowed (logged); off/over → rejected ──
    [Fact]
    public void StrategicLossException_EnabledWithinLimit_Allowed_AndFlagged()
    {
        var loss = Line("A", commissionNet: 10m, platFunded: 100m);    // deficit 90
        var r = Eval(new[] { loss }, Policy(lossEnabled: true, lossMaxDeficit: 100m));
        r.Passed.Should().BeTrue();
        r.State.Should().Be(ProfitProtectionDecisionState.Approved);
        r.Lines.Single().StrategicLossExceptionApplied.Should().BeTrue();
        r.Lines.Single().Passed.Should().BeTrue();
    }

    [Fact]
    public void StrategicLossException_Disabled_Rejected()
    {
        var loss = Line("A", commissionNet: 10m, platFunded: 100m);
        var r = Eval(new[] { loss }, Policy(lossEnabled: false));
        r.Passed.Should().BeFalse();
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.NegativeContribution);
    }

    [Fact]
    public void StrategicLossException_OverLimit_Rejected()
    {
        var loss = Line("A", commissionNet: 10m, platFunded: 100m);    // deficit 90 > limit 50
        var r = Eval(new[] { loss }, Policy(lossEnabled: true, lossMaxDeficit: 50m));
        r.Passed.Should().BeFalse();
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.NegativeContribution);
    }

    // ── (7) Missing policy / part allowance → ConfigurationError ─────────────────
    [Fact]
    public void NullPolicy_ConfigurationError()
    {
        var r = Eval(new[] { Line("A") }, policy: null);
        r.Passed.Should().BeFalse();
        r.State.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        r.PrimaryErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionConfigurationError);
    }

    [Fact]
    public void PartLine_NoResolvedAllowance_ConfigurationError()
    {
        var r = Eval(new[] { Line("A", part: true) }, Policy(), allowances: new Dictionary<string, LinePartAllowance>());
        r.Passed.Should().BeFalse();
        r.State.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.MissingAllowance);
    }

    // ── Part line takes its floor from the S5 allowance (not the policy default) ─
    [Fact]
    public void PartLine_UsesS5AllowanceFloor()
    {
        var allowances = new Dictionary<string, LinePartAllowance>
        {
            ["A"] = new LinePartAllowance(Found: true, MinimumProviderReceivable: 500m,
                AllowedProviderFundedDiscount: 0m, AllowedPlatformFundedDiscount: 0m),
        };
        // policy min-receivable is 0, but the S5 floor is 500 and providerNet is 400 → breach from the S5 floor.
        var r = Eval(new[] { Line("A", part: true, providerNet: 400m) }, Policy(minRecvAmt: 0m), allowances);
        r.Passed.Should().BeFalse();
        r.Lines.Single().Breach.Should().Be(LineProfitProtectionBreach.ProviderReceivableBelowFloor);
        r.Lines.Single().ProviderMinimumReceivableApplied.Should().Be(500m);   // came from S5, not the policy
    }
}
