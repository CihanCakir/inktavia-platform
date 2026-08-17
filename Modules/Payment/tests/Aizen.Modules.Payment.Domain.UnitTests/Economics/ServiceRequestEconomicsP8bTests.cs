using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-P8b — the combiner wired with a non-zero P6 customer discount (S6 allocation, pre-tax + tax recompute), P7 effective
/// commission, and the P5 safe-max recompute. The S8 snapshot's 8 equalities (incl. discount-total + funding-consistency)
/// hold zero-tolerance. Narrow-core (no discount, benefits off) stays byte-identical.
/// </summary>
public sealed class ServiceRequestEconomicsP8bTests
{
    private static readonly DateTime At = new(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc);

    // VAT-free lines for clean arithmetic; commission STANDARD 0.12.
    private static ServiceRequestEconomicsLine Service(decimal gross = 5000m, decimal rate = 0.12m, decimal? effRate = null, bool discountEligible = true)
        => new("L-SVC", 1, 1, LineCommissionEligibility.Eligible, gross, LineVat: 0m,
            Commissionable: true, CommissionBase: gross, ResolvedRate: rate, CommissionAmount: gross * rate,
            ProviderNet: gross - gross * rate, RuleCode: "PLAN-STD",
            DiscountEligible: discountEligible, EffectiveCommissionRate: effRate);

    private static ServiceRequestEconomicsLine Travel(decimal gross = 800m)
        => new("L-TRV", 10, 1, LineCommissionEligibility.Exempt, gross, LineVat: 0m,
            Commissionable: false, CommissionBase: 0m, ResolvedRate: 0m, CommissionAmount: 0m,
            ProviderNet: gross, RuleCode: null, DiscountEligible: false);

    // Fixed 300 platform fee → the platform's customer-side revenue is 300, so platform-funded discounts up to 300 are
    // within the P5 profit safe-max (§19.10). (A % fee would make even a modest discount unsafe — that's the adjustment path.)
    private static PlatformFeeResolution FeeRule()
        => new(3, "PF-FIX", PlatformFeeModel.Fixed, Rate: null, MinAmount: null, MaxAmount: null, FixedAmount: 300m,
            VatRate: 0.20m, SpecificityRank: 1, Priority: CommissionRulePriority.Standard, Source: "Global");
    private static PlatformFeeBreakdown FeeSeed() => new(0m, 0m, 0m, 0.20m, "Rule");

    private static ProfitProtectionPolicyEntity Policy(decimal minTxnAmt = 0m)
    {
        var p = ProfitProtectionPolicyEntity.Create("TRY", 0m, 0m, 0m, 0m, minTxnAmt, 0m, 0m, 0m, 0m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "PPOL-1");
        p.Id = 1; return p;
    }

    private static ServiceRequestCustomerDiscountInput Discount(
        decimal requested, CustomerDiscountFundingMode mode = CustomerDiscountFundingMode.PlatformFunded,
        decimal platformRate = 1m, decimal providerRate = 0m, bool consent = true, decimal budget = decimal.MaxValue)
        => new(requested, mode, platformRate, providerRate, consent, DiscountRuleId: 5, BudgetRemaining: budget);

    private static ServiceRequestEconomicsResult Combine(
        IReadOnlyList<ServiceRequestEconomicsLine> lines, ServiceRequestCustomerDiscountInput? discount = null, ProfitProtectionPolicyEntity? policy = null)
        => ServiceRequestEconomicsCombiner.Combine(42, "TRY", lines, FeeRule(), FeeSeed(), policy ?? Policy(), At, discount);

    // ── Discount end-to-end: GOLD 5% PlatformFunded on {Service 5000, Travel 800 exempt} ──

    [Fact]
    public void Discount_PlatformFunded_ReducesBase_Commission_And_PopulatesAllocation()
    {
        var r = Combine(new[] { Service(), Travel() }, Discount(250m));

        r.Decision.Should().Be(ProfitProtectionDecisionState.Approved);
        r.TotalCustomerDiscount.Should().Be(250m);
        r.TotalPlatformFundedDiscount.Should().Be(250m);
        r.TotalProviderFundedDiscount.Should().Be(0m);

        var s = r.Snapshot!;
        var svc = s.OfferLines.Single(l => l.LineRef == "L-SVC");
        svc.CustomerDiscountAmount.Should().Be(250m);
        svc.PlatformFundedDiscountAmount.Should().Be(250m);
        svc.CommissionBaseAmount.Should().Be(4750m);          // discount reduces the commission base
        svc.CommissionAmount.Should().Be(570m);               // 4750 × 0.12
        svc.ProviderNetAmount.Should().Be(4180m);             // 4750 − 570
        svc.LineTotalAmount.Should().Be(4750m);               // no VAT

        s.TotalCustomerDiscountSnapshot.Should().Be(250m);
        s.TotalPlatformFundedDiscountSnapshot.Should().Be(250m);
        // DiscountAllocation rows populated (one PlatformFunded row on the service line).
        s.DiscountAllocations.Should().ContainSingle();
        s.DiscountAllocations.Single().FundingSource.Should().Be(CustomerDiscountFundingMode.PlatformFunded);
        s.DiscountAllocations.Single().DiscountAmount.Should().Be(250m);

        // CustomerPayable = Σ post-discount LineTotal (4750 + 800); the 8 equalities held (snapshot built without throwing).
        r.CustomerPayableServiceAmount.Should().Be(5550m);
        r.ProviderNetTotal.Should().Be(4980m);                // 4180 + 800
    }

    // ── VAT recompute: discount reduces base AND VAT ────────────────────────────

    [Fact]
    public void Discount_RecomputesVat_OnReducedBase()
    {
        var svc = new ServiceRequestEconomicsLine("L1", 1, 1, LineCommissionEligibility.Eligible,
            LineGrossBeforeDiscount: 5000m, LineVat: 1000m,   // 20% VAT
            Commissionable: true, CommissionBase: 5000m, ResolvedRate: 0.12m, CommissionAmount: 600m,
            ProviderNet: 4400m, RuleCode: "STD");
        var r = Combine(new[] { svc }, Discount(250m));

        var line = r.Snapshot!.OfferLines.Single();
        line.LineVatAmount.Should().Be(950m);                 // 4750 × 0.20 (VAT recomputed on the discounted base)
        line.LineTotalAmount.Should().Be(5700m);              // 4750 + 950
    }

    // ── Funding modes + consent ─────────────────────────────────────────────────

    [Fact]
    public void Discount_ProviderFunded_ConsentYes_FundsProvider()
    {
        var r = Combine(new[] { Service(), Travel() }, Discount(250m, CustomerDiscountFundingMode.ProviderFunded, 0m, 1m, consent: true));
        r.TotalProviderFundedDiscount.Should().Be(250m);
        r.TotalPlatformFundedDiscount.Should().Be(0m);
        r.Snapshot!.DiscountAllocations.Single().FundingSource.Should().Be(CustomerDiscountFundingMode.ProviderFunded);
    }

    [Fact]
    public void Discount_ProviderFunded_NoConsent_Dropped()
    {
        var r = Combine(new[] { Service(), Travel() }, Discount(250m, CustomerDiscountFundingMode.ProviderFunded, 0m, 1m, consent: false));
        r.TotalCustomerDiscount.Should().Be(0m);              // dropped, not platform-shifted
        r.Snapshot!.OfferLines.Single(l => l.LineRef == "L-SVC").CommissionBaseAmount.Should().Be(5000m);   // full base
    }

    [Fact]
    public void Discount_Shared_SplitsFunding()
    {
        var r = Combine(new[] { Service(), Travel() }, Discount(250m, CustomerDiscountFundingMode.Shared, 0.6m, 0.4m, consent: true));
        r.TotalPlatformFundedDiscount.Should().Be(150m);      // 60%
        r.TotalProviderFundedDiscount.Should().Be(100m);      // 40%
        r.TotalCustomerDiscount.Should().Be(250m);
        r.Snapshot!.DiscountAllocations.Should().HaveCount(2);   // platform + provider rows
    }

    // ── P7 effective commission ─────────────────────────────────────────────────

    [Fact]
    public void P7_EffectiveRate_RecomputesCommission_And_BenefitCost()
    {
        // −1pp benefit: effective 0.11 on the 5000 base (no discount).
        var r = Combine(new[] { Service(effRate: 0.11m) }, discount: null);
        var svc = r.Snapshot!.OfferLines.Single();
        svc.CommissionAmount.Should().Be(550m);               // 5000 × 0.11
        svc.CommissionRate.Should().Be(0.11m);
        r.ProviderCommissionBenefitCost.Should().Be(50m);     // base 600 − effective 550
    }

    [Fact]
    public void P7_BenefitsOff_NoOp_IdenticalToBase()
    {
        var withNull = Combine(new[] { Service(effRate: null) }, discount: null);
        withNull.Snapshot!.OfferLines.Single().CommissionAmount.Should().Be(600m);   // 5000 × 0.12
        withNull.ProviderCommissionBenefitCost.Should().Be(0m);
    }

    // ── P5 safe-max: unsafe platform-funded discount → ApprovedWithAdjustment + recompute ──

    [Fact]
    public void P5_UnsafeDiscount_AdjustsToSafeMax_AndRecomputes()
    {
        // Request the whole 5000 as a platform-funded discount — far beyond the safe-max (commission + fee).
        var r = Combine(new[] { Service() }, Discount(5000m), Policy());

        r.Decision.Should().Be(ProfitProtectionDecisionState.ApprovedWithAdjustment);
        r.DiscountAdjusted.Should().BeTrue();
        r.TotalPlatformFundedDiscount.Should().BeLessThan(5000m);   // reduced to safe-max
        r.TotalPlatformFundedDiscount.Should().BeGreaterThan(0m);
        r.Snapshot.Should().NotBeNull();                            // recomputed economics still satisfy the 8 equalities
        r.TotalCustomerDiscount.Should().Be(r.TotalPlatformFundedDiscount);
    }

    // ── 8 equalities with real discounts: funding-consistency (dc == dp + dpl) is zero-tolerance ──

    private static LineEconomicsInput DiscountedLine(decimal platformFunded)
        => new("L1", 1, 1, GrossBeforeDiscount: 5000m, CustomerDiscount: 250m,
            ProviderFundedDiscount: 0m, PlatformFundedDiscount: platformFunded,
            LineCommissionEligibility.Eligible, CommissionBase: 4750m, CommissionRate: 0.12m, CommissionAmount: 570m,
            ProviderNet: 4180m, LineVat: 0m, LineTotal: 4750m, RuleId: null, RuleCode: "X", Commissionable: true);

    private static PlatformFeeInput ZeroFee() => new(null, 0m, 0m, 0m, 4750m, 0m, 0m, 0m);

    [Fact]
    public void CreateFromLines_ConsistentFunding_Ok_And_WritesAllocationRow()
    {
        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(42, "TRY", new[] { DiscountedLine(250m) }, ZeroFee(), 4750m);
        s.TotalCustomerDiscountSnapshot.Should().Be(250m);
        s.TotalPlatformFundedDiscountSnapshot.Should().Be(250m);
        s.DiscountAllocations.Should().ContainSingle();
    }

    [Fact]
    public void CreateFromLines_FundingMismatch_Throws()
    {
        // dc 250 but platform-funded 200 (+ provider 0) → funding does not sum to the customer discount.
        var act = () => PaymentEconomicsSnapshotEntity.CreateFromLines(42, "TRY", new[] { DiscountedLine(200m) }, ZeroFee(), 4750m);
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*Σ(line funding) == CustomerDiscount*");
    }

    // ── Narrow-core: no discount, benefits off → identical to BE-P8 ─────────────

    [Fact]
    public void NarrowCore_NoDiscount_ByteIdentical()
    {
        var r = Combine(new[] { Service(), Travel() }, discount: null);
        r.TotalCustomerDiscount.Should().Be(0m);
        r.Snapshot!.DiscountAllocations.Should().BeEmpty();
        r.Snapshot!.OfferLines.Single(l => l.LineRef == "L-SVC").CommissionAmount.Should().Be(600m);
        r.ProviderNetTotal.Should().Be(5200m);                // 4400 + 800
    }
}
