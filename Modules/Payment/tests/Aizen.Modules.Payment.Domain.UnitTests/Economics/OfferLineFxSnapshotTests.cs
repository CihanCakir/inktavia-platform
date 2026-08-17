using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-S3c — the frozen per-line FX record folded onto the immutable line economics snapshot (§20.7). Captured at acceptance;
/// self-contained (source ccy + price + applied rate + rate date + resolved TRY unit price); descriptive metadata that must NOT
/// move the money math or the 8 equalities. Verifies: the self-contained capture; the tamper guard (ResolvedUnitPrice must equal
/// round(SourceUnitPrice × AppliedRate)); that FX leaves the aggregate economics byte-identical; and the acceptance FREEZE —
/// the snapshot holds the submit-time rate and a later rate would produce a DIFFERENT snapshot, so the accepted one never moves.
/// </summary>
public sealed class OfferLineFxSnapshotTests
{
    // 100 EUR/unit @ 50.00 = 5_000 TRY/unit (matches the ServiceLine gross so the sample is coherent).
    private static LineFxSnapshotInput Eur(decimal rate = 50m, decimal sourceUnit = 100m)
        => new("eur", "try", sourceUnit, rate, new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
            ResolvedUnitPrice: Math.Round(sourceUnit * rate, 2, MidpointRounding.AwayFromZero));

    private static LineEconomicsInput ServiceLine(LineFxSnapshotInput? fx = null)
        => new("L-SERVICE", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: 5000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 5000m, CommissionRate: 0.12m, CommissionAmount: 600m,
            ProviderNet: 4400m, LineVat: 0m, LineTotal: 5000m,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0, Attributes: null, Fx: fx);

    private static LineEconomicsInput TravelExempt()
        => new("L-TRAVEL", 10, 1, 800m, 0m, 0m, 0m, LineCommissionEligibility.Exempt,
            0m, 0m, 0m, 800m, 0m, 800m, null, null, false, 1);

    private static PlatformFeeInput Fee() => new(3, 0.025m, 99m, 1500m, 5800m, 145m, 29m, 174m);

    // ── Self-contained FX capture (normalized, UTC rate date) ─────────────────────────────────────────
    [Fact]
    public void CreateFromLines_CapturesFrozenFx_OnTheLine()
    {
        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 42, currencyCode: "TRY",
            lines: new[] { ServiceLine(Eur(rate: 42.60m, sourceUnit: 500m)), TravelExempt() },
            platformFee: Fee(), customerPayableServiceAmount: 5800m);

        var line = s.OfferLines.Single(l => l.LineRef == "L-SERVICE");
        line.FxSourceCurrencyCode.Should().Be("EUR");        // normalized upper
        line.FxSettlementCurrencyCode.Should().Be("TRY");
        line.FxSourceUnitPrice.Should().Be(500m);
        line.FxAppliedRate.Should().Be(42.60m);
        line.FxResolvedUnitPrice.Should().Be(21_300m);       // 500 × 42.60
        line.FxRateDate!.Value.Kind.Should().Be(DateTimeKind.Utc);

        // A settlement-native line carries no FX.
        s.OfferLines.Single(l => l.LineRef == "L-TRAVEL").FxSourceCurrencyCode.Should().BeNull();
    }

    // ── Tamper guard: ResolvedUnitPrice must equal round(SourceUnitPrice × AppliedRate) ───────────────
    [Fact]
    public void CreateFromLines_TamperedResolvedPrice_Throws()
    {
        var tampered = new LineFxSnapshotInput("EUR", "TRY", 500m, 42.60m,
            new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), ResolvedUnitPrice: 9_999m);   // != 21_300

        var act = () => PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(tampered), TravelExempt() }, Fee(), 5800m);

        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*ResolvedUnitPrice*");
    }

    [Fact]
    public void CreateFromLines_SettlementEqualsSource_Throws()
    {
        var bad = new LineFxSnapshotInput("TRY", "TRY", 500m, 1m,
            new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), 500m);

        var act = () => PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(bad), TravelExempt() }, Fee(), 5800m);

        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*non-settlement*");
    }

    // ── FX is descriptive: the 8-equality aggregate economics are byte-identical with vs without FX ────
    [Fact]
    public void Fx_DoesNotChange_TheAggregateEconomics()
    {
        var withFx = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(Eur()), TravelExempt() }, Fee(), 5800m);
        var without = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(), TravelExempt() }, Fee(), 5800m);

        withFx.CommissionAmountSnapshot.Should().Be(without.CommissionAmountSnapshot);
        withFx.ProviderNetAmountSnapshot.Should().Be(without.ProviderNetAmountSnapshot);
        withFx.CustomerTotalAmountSnapshot.Should().Be(without.CustomerTotalAmountSnapshot);
        withFx.PlatformGrossShareSnapshot.Should().Be(without.PlatformGrossShareSnapshot);
        withFx.ServiceGrossAmountSnapshot.Should().Be(without.ServiceGrossAmountSnapshot);

        // Canonical smoke values still hold with FX present (same as the S2d attribute test).
        withFx.CustomerTotalAmountSnapshot.Should().Be(5974m);
        withFx.ProviderNetAmountSnapshot.Should().Be(5200m);
    }

    // ── Acceptance FREEZE: the snapshot holds the submit-time rate; a later rate is a DIFFERENT snapshot ─
    [Fact]
    public void AcceptanceSnapshot_Freezes_TheSubmitTimeRate()
    {
        var accepted = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(Eur(rate: 42.60m, sourceUnit: 500m)), TravelExempt() }, Fee(), 5800m);

        var acceptedRate = accepted.OfferLines.Single(l => l.LineRef == "L-SERVICE").FxAppliedRate;
        var acceptedTotal = accepted.CustomerTotalAmountSnapshot;

        // Simulate a ReferenceData rate change AFTER acceptance: it can only ever produce a NEW snapshot; there is no path
        // that re-resolves or mutates the accepted one (all setters private, no Update). The accepted rate/total stand.
        var later = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { ServiceLine(Eur(rate: 99.99m, sourceUnit: 500m)), TravelExempt() }, Fee(), 5800m);

        acceptedRate.Should().Be(42.60m);
        accepted.OfferLines.Single(l => l.LineRef == "L-SERVICE").FxAppliedRate.Should().Be(42.60m, "the accepted snapshot never re-values");
        accepted.CustomerTotalAmountSnapshot.Should().Be(acceptedTotal);
        later.OfferLines.Single(l => l.LineRef == "L-SERVICE").FxAppliedRate.Should().Be(99.99m, "a re-run is a distinct object");
    }
}
