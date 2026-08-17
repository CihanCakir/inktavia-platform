using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.ProfitProtection;

public sealed class ProfitProtectionEngineTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProfitProtectionPolicyEntity Policy(
        decimal minCustAmt = 0m, decimal minCustRate = 0m,
        decimal minProvAmt = 0m, decimal minProvRate = 0m,
        decimal minTxnAmt  = 0m, decimal minTxnRate  = 0m,
        decimal procRate = 0m, decimal procFixed = 0m,
        decimal refundRate = 0m, decimal otherRate = 0m, decimal otherFixed = 0m,
        decimal custVarShare = 0.5m,
        ProfitProtectionAdjustmentOrder order = ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
        long id = 1)
    {
        var p = ProfitProtectionPolicyEntity.Create(
            "TRY", minCustAmt, minCustRate, minProvAmt, minProvRate, minTxnAmt, minTxnRate,
            procRate, procFixed, refundRate, otherRate, otherFixed, custVarShare, order, From, null, "PPOL-1");
        p.Id = id;
        return p;
    }

    private static ProfitProtectionContext Ctx(
        decimal serviceAmount = 1000m, decimal custPayable = 1000m, decimal custTotal = 1120m, decimal providerNet = 850m,
        decimal commissionNet = 150m, decimal platformFeeNet = 120m,
        decimal requestedDiscount = 0m, decimal benefitCost = 0m, decimal budget = 0m)
        => new("TRY", serviceAmount, custPayable, custTotal, providerNet, commissionNet, platformFeeNet,
               RequestedPlatformFundedCustomerDiscount: requestedDiscount,
               ProviderCommissionBenefitCost: benefitCost,
               CustomerBenefitBudgetRemaining: budget);

    // ── Three contributions (§19.2) ──────────────────────────────────────────────

    [Fact]
    public void Computes_The_Three_Contributions_From_Context()
    {
        var ev = ProfitProtectionEngine.Evaluate(Ctx(), Policy());   // no expenses, no minimums

        ev.CustomerSideContributionExpected.Should().Be(120m);      // platform fee net
        ev.ProviderSideContributionExpected.Should().Be(150m);      // commission net
        ev.TotalTransactionContributionExpected.Should().Be(270m);  // 150 + 120
        ev.State.Should().Be(ProfitProtectionDecisionState.Approved);
    }

    [Fact]
    public void Required_Is_Max_Of_Amount_And_Base_Times_Rate()
    {
        // reqTxn = Max(10, Round(1120 × 0.01 = 11.20)) = 11.20 (rate wins).
        var ev = ProfitProtectionEngine.Evaluate(Ctx(), Policy(minTxnAmt: 10m, minTxnRate: 0.01m));
        ev.RequiredTransactionContribution.Should().Be(11.20m);

        // reqTxn = Max(50, Round(1120 × 0.01)) = 50 (amount wins).
        var ev2 = ProfitProtectionEngine.Evaluate(Ctx(), Policy(minTxnAmt: 50m, minTxnRate: 0.01m));
        ev2.RequiredTransactionContribution.Should().Be(50m);
    }

    // ── All gates pass → Approved; each gate failing → not Approved ──────────────

    [Fact]
    public void All_Gates_Pass_Yields_Approved()
        => ProfitProtectionEngine.Evaluate(Ctx(), Policy()).State
            .Should().Be(ProfitProtectionDecisionState.Approved);

    [Theory]
    [InlineData(500, 0, 0)]   // customer gate: reqCustomer 500 > 120
    [InlineData(0, 500, 0)]   // provider gate: reqProvider 500 > 150
    [InlineData(0, 0, 500)]   // total gate:    reqTransaction 500 > 270
    public void Each_Gate_Failing_Individually_Is_Not_Approved(decimal custAmt, decimal provAmt, decimal txnAmt)
    {
        var ev = ProfitProtectionEngine.Evaluate(Ctx(),
            Policy(minCustAmt: custAmt, minProvAmt: provAmt, minTxnAmt: txnAmt));
        ev.State.Should().NotBe(ProfitProtectionDecisionState.Approved);
        ev.State.Should().Be(ProfitProtectionDecisionState.Rejected);
    }

    // ── Safe-max discount (§19.10) + budget cap + CustomerTotal ≥ ProviderNet ────

    [Fact]
    public void Requested_Discount_Above_SafeMax_Yields_ApprovedWithAdjustment_At_SafeMax()
    {
        // No expenses/minimums. txnRevenue = 270 → §19.10 MaximumSafe (total-based) = 270.
        // Applied also respects the customer gate (bound = 120). Requested 1000 → adjusted down.
        var ev = ProfitProtectionEngine.Evaluate(Ctx(requestedDiscount: 1000m, budget: 5000m), Policy());

        ev.State.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
        ev.MaximumSafePlatformFundedDiscount.Should().Be(270m);        // §19.10 total-based figure
        ev.AppliedPlatformFundedDiscount.Should().Be(120m);            // capped by the customer gate
        ev.AppliedPlatformFundedDiscount.Should().BeLessThan(1000m);
        ev.AdjustmentReason.Should().Contain("platform-funded discount");
    }

    [Fact]
    public void SafeMax_Is_Clamped_To_The_Customer_Benefit_Budget()
    {
        var ev = ProfitProtectionEngine.Evaluate(Ctx(requestedDiscount: 1000m, budget: 50m), Policy());
        ev.MaximumSafePlatformFundedDiscount.Should().Be(50m);         // clamped to budget
        ev.AppliedPlatformFundedDiscount.Should().Be(50m);
        ev.State.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
    }

    [Fact]
    public void SafeMax_Keeps_CustomerTotal_At_Least_ProviderNet()
    {
        // custTotal 900, providerNet 850 → net headroom for extra discount = 900 + requested(200) − 850 = 250.
        var ev = ProfitProtectionEngine.Evaluate(
            Ctx(custTotal: 900m, providerNet: 850m, requestedDiscount: 200m, budget: 5000m), Policy());
        ev.MaximumSafePlatformFundedDiscount.Should().Be(250m);        // net constraint binds (< total-based 270)
    }

    // ── Rejected: no safe combination even at zero advantage ─────────────────────

    [Fact]
    public void No_Safe_Combination_Yields_Rejected()
    {
        // reqCustomer 500 > customer revenue 120 even with zero discount → unfixable → Rejected.
        var ev = ProfitProtectionEngine.Evaluate(Ctx(requestedDiscount: 100m, budget: 5000m), Policy(minCustAmt: 500m));
        ev.State.Should().Be(ProfitProtectionDecisionState.Rejected);
        ev.AppliedPlatformFundedDiscount.Should().Be(0m);
    }

    // ── ConfigurationError: no policy ────────────────────────────────────────────

    [Fact]
    public void No_Policy_Yields_ConfigurationError()
    {
        var ev = ProfitProtectionEngine.Evaluate(Ctx(), policy: null);
        ev.State.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        ev.PolicyId.Should().BeNull();
    }

    // ── §19.1 loss example → NOT Approved (adjusted away from the −250 loss) ─────

    [Fact]
    public void Section_19_1_Loss_Example_Is_Not_Approved()
    {
        // Service 10 000; commission net 900; platform fee net 250; requested discount 1 000; expected cost 400.
        // Raw (full-discount) contribution = 900 + 250 − 1000 − 400 = −250 (loss). The engine must NOT approve it as-is.
        var ctx = Ctx(serviceAmount: 10000m, custTotal: 9250m, providerNet: 9100m,
            commissionNet: 900m, platformFeeNet: 250m, requestedDiscount: 1000m, budget: 5000m);
        var ev = ProfitProtectionEngine.Evaluate(ctx, Policy(procFixed: 400m));

        ev.State.Should().NotBe(ProfitProtectionDecisionState.Approved);
        ev.State.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
        ev.AppliedPlatformFundedDiscount.Should().BeLessThan(1000m);
        ev.TotalTransactionContributionExpected.Should().BeGreaterThanOrEqualTo(ev.RequiredTransactionContribution);
    }

    // ── Purity / determinism ─────────────────────────────────────────────────────

    [Fact]
    public void Same_Context_And_Policy_Yield_Identical_Evaluation()
    {
        var ctx = Ctx(requestedDiscount: 300m, budget: 1000m);
        var policy = Policy(minTxnAmt: 5m, procRate: 0.02m, procFixed: 1m);

        var a = ProfitProtectionEngine.Evaluate(ctx, policy);
        var b = ProfitProtectionEngine.Evaluate(ctx, policy);

        a.Should().Be(b);   // record value-equality → deterministic, side-effect-free
    }

    // ── FIX_OFFER_LINE_PRICING_ON_CREATE regression guard ────────────────────────
    // A realistic, PROPERLY-PRICED offer clears §19.2 on BOTH sides at the SEEDED policy (cost-share 0.5) — so a future
    // single-knob policy tweak can't silently block all accepts. A DEGENERATE ₺0-service offer (un-priced lines →
    // revenue = platform-fee floor only) stays correctly rejected (this was the "provider −2.14" accept-gate).
    [Fact]
    public void SeededPolicy_Approves_Priced_Offer_And_Rejects_Degenerate_ZeroService()
    {
        // Seeded active TRY policy values (payment.profit_protection_policies id 1).
        var seeded = Policy(minTxnAmt: 10m, minTxnRate: 0.01m,
                            procRate: 0.029m, procFixed: 0.25m, refundRate: 0.005m, custVarShare: 0.5m);

        // Realistic: ₺1000 service (payable 1200), platform-fee floor 99 → CustomerTotal 1318.80, commission 150, net 850.
        var priced = ProfitProtectionEngine.Evaluate(
            Ctx(serviceAmount: 1000m, custPayable: 1200m, custTotal: 1318.80m, providerNet: 850m,
                commissionNet: 150m, platformFeeNet: 99m), seeded);
        priced.State.Should().Be(ProfitProtectionDecisionState.Approved);
        priced.ProviderSideContributionExpected.Should().BeGreaterThan(0m);
        priced.CustomerSideContributionExpected.Should().BeGreaterThan(0m);

        // Degenerate: un-priced lines → ₺0 service; only the platform-fee floor (feeGross 118.80) is revenue.
        var degenerate = ProfitProtectionEngine.Evaluate(
            Ctx(serviceAmount: 0m, custPayable: 0m, custTotal: 118.80m, providerNet: 0m,
                commissionNet: 0m, platformFeeNet: 99m), seeded);
        degenerate.State.Should().Be(ProfitProtectionDecisionState.Rejected);
        degenerate.ProviderSideContributionExpected.Should().BeLessThan(0m);   // the −2.14 breach
    }
}
