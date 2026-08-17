using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-P8 — the pure §19.9 combiner. Fuses S7 line commission + P3 platform fee + P5 policy into the binding order,
/// gates zero-tolerance, and (on approval) builds the S8 immutable snapshot (8 equalities). No I/O.
/// </summary>
public sealed class ServiceRequestEconomicsCombinerTests
{
    private static readonly DateTime At = new(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc);

    // Provider on STANDARD (0.12): Service 5000 eligible → commission 600, net 4400. Travel 800 exempt → 0, net 800.
    private static ServiceRequestEconomicsLine ServiceLine(decimal gross = 5000m, decimal rate = 0.12m, decimal commission = 600m)
        => new("L-SVC", ItemType: 1, PricingMethod: 1, LineCommissionEligibility.Eligible,
            LineGrossBeforeDiscount: gross, LineVat: 0m,
            Commissionable: true, CommissionBase: gross, ResolvedRate: rate, CommissionAmount: commission,
            ProviderNet: gross - commission, RuleCode: "PLAN-STD");

    private static ServiceRequestEconomicsLine TravelExempt(decimal gross = 800m)
        => new("L-TRV", ItemType: 10, PricingMethod: 1, LineCommissionEligibility.Exempt,
            LineGrossBeforeDiscount: gross, LineVat: 0m,
            Commissionable: false, CommissionBase: 0m, ResolvedRate: 0m, CommissionAmount: 0m,
            ProviderNet: gross, RuleCode: null);

    // Platform fee: 2.5% of 5800 = 145 net, 20% VAT = 29, gross 174.
    private static PlatformFeeResolution FeeRule()
        => new(RuleId: 3, RuleCode: "PF-STD", Model: PlatformFeeModel.PercentageWithBounds,
            Rate: 0.025m, MinAmount: 99m, MaxAmount: 1500m, FixedAmount: null, VatRate: 0.20m,
            SpecificityRank: 1, Priority: CommissionRulePriority.Standard, Source: "Global");

    private static PlatformFeeBreakdown Fee(decimal net = 145m, decimal vat = 29m, decimal gross = 174m)
        => new(net, vat, gross, VatRate: 0.20m, VatSource: "Rule");

    private static ProfitProtectionPolicyEntity Policy(
        decimal minCustAmt = 0m, decimal minProvAmt = 0m, decimal minTxnAmt = 0m, long id = 1)
    {
        var p = ProfitProtectionPolicyEntity.Create(
            "TRY", minCustAmt, 0m, minProvAmt, 0m, minTxnAmt, 0m,
            0m, 0m, 0m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "PPOL-1");
        p.Id = id;
        return p;
    }

    private static ServiceRequestEconomicsResult Combine(
        IReadOnlyList<ServiceRequestEconomicsLine>? lines = null,
        PlatformFeeBreakdown? fee = null,
        ProfitProtectionPolicyEntity? policy = null,
        bool defaultPolicy = true)
        => ServiceRequestEconomicsCombiner.Combine(
            serviceRequestId: 42, currencyCode: "TRY",
            lines: lines ?? new[] { ServiceLine(), TravelExempt() },
            platformFeeRule: FeeRule(), platformFee: fee ?? Fee(),
            policy: policy ?? (defaultPolicy ? Policy() : null),
            createdAtUtc: At);

    // ── Happy path — amounts + snapshot + 8 equalities ──────────────────────────

    [Fact]
    public void Combine_HappyPath_ProducesAmountsAndSnapshot()
    {
        var r = Combine();

        r.Decision.Should().Be(ProfitProtectionDecisionState.Approved);
        r.OriginalServiceGrossAmount.Should().Be(5800m);
        r.TransactionCommission.Should().Be(600m);
        r.ProviderNetTotal.Should().Be(5200m);
        r.CustomerPayableServiceAmount.Should().Be(5800m);
        r.PlatformFeeGross.Should().Be(174m);
        r.CustomerTotalAmount.Should().Be(5974m);         // 5800 + 174
        r.AppliedRuleCodes.Should().ContainSingle().Which.Should().Be("PLAN-STD");

        r.Snapshot.Should().NotBeNull();
        var s = r.Snapshot!;
        s.ContextId.Should().Be(42);
        s.CommissionAmountSnapshot.Should().Be(600m);
        s.ProviderNetAmountSnapshot.Should().Be(5200m);
        s.CustomerTotalAmountSnapshot.Should().Be(5974m);
        s.OriginalServiceGrossAmountSnapshot.Should().Be(5800m);
        s.OfferLines.Should().HaveCount(2);
        s.CommissionAllocations.Should().HaveCount(2);
        s.DiscountAllocations.Should().BeEmpty();

        // S8's 8 equalities (structural double-check)
        (s.OfferLines.Sum(l => l.LineTotalAmount) + s.PlatformFeeGrossAmountSnapshot)
            .Should().Be(s.CustomerTotalAmountSnapshot);
        (s.ProviderNetAmountSnapshot + s.PlatformGrossShareSnapshot).Should().Be(s.CustomerTotalAmountSnapshot);
        (s.PlatformFeeNetAmountSnapshot + s.PlatformFeeVatAmountSnapshot).Should().Be(s.PlatformFeeGrossAmountSnapshot);
    }

    // ── Gateway amounts: escrow gross = CustomerTotal, split = ProviderNet ───────

    [Fact]
    public void Combine_GatewayAmounts_CustomerTotalGross_ProviderNetSplit()
    {
        var r = Combine();
        r.CustomerTotalAmount.Should().Be(5974m);   // charged to customer (NOT raw provider price 5800)
        r.ProviderNetTotal.Should().Be(5200m);      // provider split target
        r.CustomerTotalAmount.Should().BeGreaterThan(r.ProviderNetTotal);
    }

    // ── Reject → no snapshot ────────────────────────────────────────────────────

    [Fact]
    public void Combine_ProfitProtectionRejects_NoSnapshot()
    {
        // provider gate requires 5000 but commission net is only 600 → Rejected.
        var r = Combine(policy: Policy(minProvAmt: 5000m));

        r.Decision.Should().Be(ProfitProtectionDecisionState.Rejected);
        r.Snapshot.Should().BeNull();
        r.CanProceed.Should().BeFalse();
        r.AppliedRuleCodes.Should().BeEmpty();
        // amounts still reported for the reason/telemetry
        r.CustomerTotalAmount.Should().Be(5974m);
    }

    // ── ConfigurationError (no policy) → no snapshot ────────────────────────────

    [Fact]
    public void Combine_NoPolicy_ConfigurationError_NoSnapshot()
    {
        var r = Combine(defaultPolicy: false);

        r.Decision.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        r.Snapshot.Should().BeNull();
        r.CanProceed.Should().BeFalse();
    }

    // ── Plan rate flows into commission (STANDARD 0.12 vs PREMIUM 0.09) ──────────

    [Theory]
    [InlineData(0.12, 600, 4400)]   // STANDARD
    [InlineData(0.09, 450, 4550)]   // PREMIUM
    [InlineData(0.15, 750, 4250)]   // FREE / global
    public void Combine_UsesResolvedRate_ForCommissionAndNet(decimal rate, decimal commission, decimal net)
    {
        var svc = ServiceLine(rate: rate, commission: commission);
        var r = Combine(lines: new[] { svc, TravelExempt() });

        r.TransactionCommission.Should().Be(commission);
        r.ProviderNetTotal.Should().Be(net + 800m);      // + travel net 800
        r.Snapshot!.CommissionAmountSnapshot.Should().Be(commission);
    }

    // ── §19.8 resolver independence: the discount seam (0) does not alter commission ─

    [Fact]
    public void Combine_ResolverIndependence_ZeroDiscountDoesNotTouchCommission()
    {
        var r = Combine();
        r.Snapshot!.TotalCustomerDiscountSnapshot.Should().Be(0m);
        r.Snapshot!.TotalProviderFundedDiscountSnapshot.Should().Be(0m);
        r.Snapshot!.TotalPlatformFundedDiscountSnapshot.Should().Be(0m);
        // commission unchanged by the (zero) discount inputs
        r.TransactionCommission.Should().Be(600m);
    }

    // ── Purity / determinism ────────────────────────────────────────────────────

    [Fact]
    public void Combine_IsDeterministic()
    {
        var a = Combine();
        var b = Combine();
        a.CustomerTotalAmount.Should().Be(b.CustomerTotalAmount);
        a.ProviderNetTotal.Should().Be(b.ProviderNetTotal);
        a.TransactionCommission.Should().Be(b.TransactionCommission);
        a.Snapshot!.CommissionRateSnapshot.Should().Be(b.Snapshot!.CommissionRateSnapshot);
    }

    // ── VAT path: customerPayable = Σ LineTotal (VAT-inclusive) ──────────────────

    [Fact]
    public void Combine_WithVat_CustomerPayableIsVatInclusive()
    {
        // Service net 1000 + VAT 200; commission 150 @0.15; provider net 850.
        var svc = new ServiceRequestEconomicsLine("L1", 1, 1, LineCommissionEligibility.Eligible,
            LineGrossBeforeDiscount: 1000m, LineVat: 200m,
            Commissionable: true, CommissionBase: 1000m, ResolvedRate: 0.15m, CommissionAmount: 150m,
            ProviderNet: 850m, RuleCode: "STD");

        // BE-P8b: the fee is now recomputed on the (post-discount) payable from the RULE (min 99 clamp applies on 1200):
        // net 99, vat 19.80, gross 118.80 → CustomerTotal 1318.80. (The passed breakdown supplies only VatRate/VatSource.)
        var r = ServiceRequestEconomicsCombiner.Combine(
            42, "TRY", new[] { svc }, FeeRule(), Fee(net: 30m, vat: 6m, gross: 36m), Policy(), At);

        r.CustomerPayableServiceAmount.Should().Be(1200m);   // 1000 + 200
        r.PlatformFeeGross.Should().Be(118.80m);             // rule min 99 → 99 + 19.80 VAT
        r.CustomerTotalAmount.Should().Be(1318.80m);         // 1200 + 118.80
        r.Snapshot!.ServiceAmountSnapshot.Should().Be(1000m);
        r.Snapshot!.ServiceVatAmountSnapshot.Should().Be(200m);
    }

    // ── Empty lines guard ───────────────────────────────────────────────────────

    [Fact]
    public void Combine_EmptyLines_Throws()
    {
        var act = () => ServiceRequestEconomicsCombiner.Combine(
            42, "TRY", Array.Empty<ServiceRequestEconomicsLine>(), FeeRule(), Fee(), Policy(), At);
        act.Should().Throw<PaymentEconomicsInvariantException>();
    }
}
