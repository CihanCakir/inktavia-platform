using System.Reflection;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-S4b (§20.8) — the immutable travel/mobilization snapshot, a child of the aggregate economics snapshot (fills the reserved
/// slot). Captured at acceptance; self-contained (method, origin/destination codes + denormalized labels, provider-declared
/// distance, per-km rate, KILOMETER unit, resolved travel amount). Descriptive — it must NOT move the money math or the 8
/// equalities. Verifies: PerKm + FlatMobilization capture; the tamper guard (resolved amount == Round(km × rate)); that travel
/// leaves the aggregate economics byte-identical; immutability (no public setters); and the acceptance FREEZE.
/// </summary>
public sealed class TravelPricingSnapshotTests
{
    private static LineEconomicsInput ServiceLine()
        => new("L-SERVICE", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: 5000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 5000m, CommissionRate: 0.12m, CommissionAmount: 600m,
            ProviderNet: 4400m, LineVat: 0m, LineTotal: 5000m,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0);

    // A Travel line (Exempt pass-through) priced PerKm: gross = km × rate.
    private static LineEconomicsInput TravelPerKm(decimal km = 40m, decimal rate = 25m, TravelSnapshotInput? travel = null)
    {
        var gross = km * rate;
        return new("L-TRAVEL", ItemType: 10, PricingMethod: 2 /*PerKm*/,
            GrossBeforeDiscount: gross, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Exempt,
            CommissionBase: 0m, CommissionRate: 0m, CommissionAmount: 0m,
            ProviderNet: gross, LineVat: 0m, LineTotal: gross,
            RuleId: null, RuleCode: null, Commissionable: false, SortOrder: 1, Travel: travel);
    }

    private static LineEconomicsInput TravelFlat(decimal fee = 750m, TravelSnapshotInput? travel = null)
        => new("L-TRAVEL", ItemType: 10, PricingMethod: 1 /*Fixed*/,
            GrossBeforeDiscount: fee, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Exempt,
            CommissionBase: 0m, CommissionRate: 0m, CommissionAmount: 0m,
            ProviderNet: fee, LineVat: 0m, LineTotal: fee,
            RuleId: null, RuleCode: null, Commissionable: false, SortOrder: 1, Travel: travel);

    private static TravelSnapshotInput PerKmInput(decimal km = 40m, decimal rate = 25m)
        => new(Method: 2, "IZM", "İzmir", "IST", "İstanbul", km, rate, "KILOMETER");

    private static TravelSnapshotInput FlatInput()
        => new(Method: 1, "IZM", "İzmir", "IST", "İstanbul", null, null, null);

    private static PlatformFeeInput Fee() => new(3, 0.025m, 99m, 1500m, 5800m, 145m, 29m, 174m);

    // ── PerKm capture (40 km × ₺25) ───────────────────────────────────────────────
    [Fact]
    public void CreateFromLines_CapturesTravelSnapshot_PerKm()
    {
        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 42, currencyCode: "TRY",
            lines: new[] { ServiceLine(), TravelPerKm(travel: PerKmInput()) },
            platformFee: Fee(), customerPayableServiceAmount: 6000m);   // ΣLineTotal = 5000 + 1000

        s.TravelPricingSnapshots.Should().HaveCount(1);
        var t = s.TravelPricingSnapshots.Single();
        t.LineRef.Should().Be("L-TRAVEL");
        t.Method.Should().Be(2);
        t.DistanceKm.Should().Be(40m);
        t.PerKmRate.Should().Be(25m);
        t.UnitCode.Should().Be("KILOMETER");
        t.OriginCityCode.Should().Be("IZM");
        t.OriginCityLabel.Should().Be("İzmir");
        t.DestinationCityLabel.Should().Be("İstanbul");
        t.ResolvedTravelAmount.Should().Be(1000m);   // = the Travel line total (40 × 25)
    }

    // ── FlatMobilization capture (₺750) ───────────────────────────────────────────
    [Fact]
    public void CreateFromLines_CapturesTravelSnapshot_Flat()
    {
        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelFlat(travel: FlatInput()) }, Fee(), 5750m);

        var t = s.TravelPricingSnapshots.Single();
        t.Method.Should().Be(1);
        t.DistanceKm.Should().BeNull();
        t.PerKmRate.Should().BeNull();
        t.UnitCode.Should().BeNull();
        t.ResolvedTravelAmount.Should().Be(750m);   // the flat fee
    }

    // ── Tamper guard: resolved amount (line gross) must equal Round(distanceKm × perKmRate) ────────────
    [Fact]
    public void CreateFromLines_TamperedTravelAmount_Throws()
    {
        // Line gross 999 but the derivation claims 40 × 25 = 1000 → the factory rejects the mismatch.
        var badTravel = new LineEconomicsInput("L-TRAVEL", 10, 2, 999m, 0m, 0m, 0m,
            LineCommissionEligibility.Exempt, 0m, 0m, 0m, 999m, 0m, 999m, null, null, false, 1,
            Travel: PerKmInput(40m, 25m));

        var act = () => PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), badTravel }, Fee(), 5999m);

        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*ResolvedTravelAmount*");
    }

    [Fact]
    public void Factory_Flat_WithDistance_Throws()
    {
        var act = () => TravelPricingSnapshotEntity.Create(
            "L-TRAVEL", 1, null, null, null, null, distanceKm: 10m, perKmRate: null, unitCode: null, resolvedTravelAmount: 750m);
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*must not carry a distance*");
    }

    [Fact]
    public void Factory_PerKm_NonKilometer_Throws()
    {
        var act = () => TravelPricingSnapshotEntity.Create(
            "L-TRAVEL", 2, null, null, null, null, distanceKm: 40m, perKmRate: 25m, unitCode: "LITER", resolvedTravelAmount: 1000m);
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*KILOMETER*");
    }

    // ── Travel is descriptive: the 8-equality aggregate economics are byte-identical with vs without it ─
    [Fact]
    public void Travel_DoesNotChange_TheAggregateEconomics()
    {
        var withTravel = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelPerKm(travel: PerKmInput()) }, Fee(), 6000m);
        var without = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelPerKm(travel: null) }, Fee(), 6000m);

        without.TravelPricingSnapshots.Should().BeEmpty();
        withTravel.CommissionAmountSnapshot.Should().Be(without.CommissionAmountSnapshot);
        withTravel.ProviderNetAmountSnapshot.Should().Be(without.ProviderNetAmountSnapshot);
        withTravel.CustomerTotalAmountSnapshot.Should().Be(without.CustomerTotalAmountSnapshot);
        withTravel.PlatformGrossShareSnapshot.Should().Be(without.PlatformGrossShareSnapshot);
        withTravel.ServiceGrossAmountSnapshot.Should().Be(without.ServiceGrossAmountSnapshot);

        // Canonical values hold: CustomerTotal = ΣLineTotal(6000) + feeGross(174); ProviderNet = 4400 + 1000.
        withTravel.CustomerTotalAmountSnapshot.Should().Be(6174m);
        withTravel.ProviderNetAmountSnapshot.Should().Be(5400m);
    }

    // ── Immutability: no public setters on the S4 snapshot (append-only) ───────────────────────────────
    [Fact]
    public void TravelPricingSnapshot_Has_No_Public_Setters()
    {
        var props = typeof(TravelPricingSnapshotEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        props.Where(p => p.SetMethod is { IsPublic: true }).Should().BeEmpty();
    }

    // ── Acceptance FREEZE: the snapshot holds the accepted amount; a later re-run is a distinct object ──
    [Fact]
    public void AcceptanceSnapshot_Freezes_TheTravelAmount()
    {
        var accepted = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelPerKm(km: 40m, rate: 25m, travel: PerKmInput(40m, 25m)) }, Fee(), 6000m);
        var acceptedAmount = accepted.TravelPricingSnapshots.Single().ResolvedTravelAmount;

        // A later rate/city change AFTER acceptance can only ever produce a NEW snapshot; the accepted one never re-values.
        var later = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelPerKm(km: 40m, rate: 30m, travel: PerKmInput(40m, 30m)) }, Fee(), 6200m);

        acceptedAmount.Should().Be(1000m);
        accepted.TravelPricingSnapshots.Single().ResolvedTravelAmount.Should().Be(1000m, "the accepted snapshot never re-values");
        later.TravelPricingSnapshots.Single().ResolvedTravelAmount.Should().Be(1200m, "a re-run is a distinct object");
    }
}
