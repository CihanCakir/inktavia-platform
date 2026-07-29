using System.Reflection;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Money;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-S8 — the line-first factory <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/> (§20.15). Every aggregate
/// is a line sum; the 8 equalities are enforced with zero tolerance; the aggregate commission is the sum of the rounded
/// line commissions (not a re-rounded blended figure); the aggregate rate is reporting-only.
/// </summary>
public sealed class PaymentEconomicsSnapshotFromLinesTests
{
    // ── Canonical smoke line set (no VAT) — Service 5000@0.12 + Travel 800 exempt ─
    private static LineEconomicsInput ServiceLine(
        decimal gross = 5000m, decimal rate = 0.12m, decimal commission = 600m, string lineRef = "L-SERVICE")
        => new(lineRef, ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: gross, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: gross, CommissionRate: rate, CommissionAmount: commission,
            ProviderNet: gross - commission, LineVat: 0m, LineTotal: gross,
            RuleId: 7, RuleCode: "PLAN-STD", Commissionable: true, SortOrder: 0);

    private static LineEconomicsInput TravelLineExempt(decimal gross = 800m, string lineRef = "L-TRAVEL")
        => new(lineRef, ItemType: 10, PricingMethod: 1,
            GrossBeforeDiscount: gross, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Exempt,
            CommissionBase: 0m, CommissionRate: 0m, CommissionAmount: 0m,
            ProviderNet: gross, LineVat: 0m, LineTotal: gross,
            RuleId: null, RuleCode: null, Commissionable: false, SortOrder: 1);

    // Platform fee 2.5% of 5800 = 145 net, 20% VAT = 29, gross 174.
    private static PlatformFeeInput Fee(decimal net = 145m, decimal vat = 29m, decimal gross = 174m)
        => new(RuleId: 3, Rate: 0.025m, Minimum: 99m, Maximum: 1500m, Base: 5800m, Net: net, Vat: vat, Gross: gross);

    private static PaymentEconomicsSnapshotEntity CreateSmoke()
        => PaymentEconomicsSnapshotEntity.CreateFromLines(
            contextId: 42, currencyCode: "TRY",
            lines: new[] { ServiceLine(), TravelLineExempt() },
            platformFee: Fee(),
            customerPayableServiceAmount: 5800m,
            createdAtUtc: new DateTime(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc));

    // ── Smoke: aggregates derived from line sums; all 8 equalities hold ──────────

    [Fact]
    public void CreateFromLines_Smoke_DerivesAggregatesFromLineSums()
    {
        var s = CreateSmoke();

        s.OriginalServiceGrossAmountSnapshot.Should().Be(5800m);   // Σ gross
        s.CommissionAmountSnapshot.Should().Be(600m);              // Σ line commission (binding)
        s.ProviderNetAmountSnapshot.Should().Be(5200m);            // Σ line provider net
        s.ServiceAmountSnapshot.Should().Be(5800m);                // Σ (lineTotal − vat)
        s.ServiceVatTotalSnapshot.Should().Be(0m);
        s.ServiceGrossAmountSnapshot.Should().Be(5800m);
        s.CustomerPayableServiceAmountSnapshot.Should().Be(5800m);
        s.CustomerTotalAmountSnapshot.Should().Be(5974m);          // Σ lineTotal + feeGross
        s.PlatformGrossShareSnapshot.Should().Be(774m);            // customerTotal − providerNet
        s.CommissionRateSnapshot.Should().Be(0.12m);               // reporting: 600 / 5000
        s.CommissionBaseAmountSnapshot.Should().Be(5000m);

        // §20.15 equalities
        (s.OfferLines.Sum(l => l.LineTotalAmount) + s.PlatformFeeGrossAmountSnapshot)
            .Should().Be(s.CustomerTotalAmountSnapshot);
        (s.ServiceAmountSnapshot + s.ServiceVatAmountSnapshot).Should().Be(s.ServiceGrossAmountSnapshot);
        (s.ProviderNetAmountSnapshot + s.PlatformGrossShareSnapshot).Should().Be(s.CustomerTotalAmountSnapshot);
        s.OfferLines.Sum(l => l.CommissionAmount).Should().Be(s.CommissionAmountSnapshot);
        s.OfferLines.Sum(l => l.ProviderNetAmount).Should().Be(s.ProviderNetAmountSnapshot);
    }

    [Fact]
    public void CreateFromLines_AttachesLineChildren_NoDiscountRowsInNarrowCore()
    {
        var s = CreateSmoke();

        s.OfferLines.Should().HaveCount(2);
        s.CommissionAllocations.Should().HaveCount(2);
        s.DiscountAllocations.Should().BeEmpty("the narrow core has no funded discounts");

        var commissionable = s.CommissionAllocations.Single(c => c.LineRef == "L-SERVICE");
        commissionable.CommissionRuleId.Should().Be(7);
        commissionable.CommissionRuleCode.Should().Be("PLAN-STD");
        commissionable.Commissionable.Should().BeTrue();

        var exempt = s.CommissionAllocations.Single(c => c.LineRef == "L-TRAVEL");
        exempt.Commissionable.Should().BeFalse();
        exempt.CommissionAmount.Should().Be(0m);
    }

    // ── VAT-inclusive path: aggregate maps net/vat/gross correctly ───────────────

    [Fact]
    public void CreateFromLines_WithVat_MapsNetVatGross()
    {
        // Service net 1000 + VAT 200 → lineTotal 1200; commission 15% of 1000 = 150; providerNet 850.
        var line = new LineEconomicsInput("L1", 1, 1,
            GrossBeforeDiscount: 1000m, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: 1000m, CommissionRate: 0.15m, CommissionAmount: 150m,
            ProviderNet: 850m, LineVat: 200m, LineTotal: 1200m,
            RuleId: 7, RuleCode: "STD", Commissionable: true);

        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", new[] { line },
            new PlatformFeeInput(null, 0m, 0m, 0m, 0m, 100m, 20m, 120m),
            customerPayableServiceAmount: 1200m);

        s.ServiceAmountSnapshot.Should().Be(1000m);       // lineTotal 1200 − vat 200
        s.ServiceVatAmountSnapshot.Should().Be(200m);
        s.ServiceGrossAmountSnapshot.Should().Be(1200m);
        s.ProviderNetAmountSnapshot.Should().Be(850m);    // 1000 − 150
        s.CustomerTotalAmountSnapshot.Should().Be(1320m); // 1200 + 120
        s.PlatformGrossShareSnapshot.Should().Be(470m);   // 1320 − 850
    }

    // ── Commission = Σ line-sum, NOT Round(ΣBase × blended); rate reporting-only ─

    [Fact]
    public void CreateFromLines_Commission_IsLineSum_NotBlendedReround()
    {
        // 3 lines, each base 1.00 @ 0.125 → per-line Round(0.125)=0.13. Σ = 0.39.
        // An aggregate re-round at the true 0.125 would give Round(3.00×0.125)=0.38 — divergent by 0.01.
        var lines = Enumerable.Range(0, 3).Select(i => new LineEconomicsInput(
            $"L{i}", 1, 1, 1.00m, 0m, 0m, 0m,
            LineCommissionEligibility.Eligible,
            CommissionBase: 1.00m, CommissionRate: 0.125m, CommissionAmount: 0.13m,
            ProviderNet: 0.87m, LineVat: 0m, LineTotal: 1.00m,
            RuleId: 7, RuleCode: "STD", Commissionable: true)).ToArray();

        var s = PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", lines,
            new PlatformFeeInput(null, 0m, 0m, 0m, 0m, 0m, 0m, 0m),
            customerPayableServiceAmount: 3.00m);

        s.CommissionAmountSnapshot.Should().Be(0.39m);            // line-sum (binding)
        MoneyMath.Round(3.00m * 0.125m).Should().Be(0.38m);       // blended would diverge
        s.CommissionAmountSnapshot.Should().NotBe(0.38m);
        s.CommissionRateSnapshot.Should().Be(0.1300m);            // reporting: 0.39 / 3.00
    }

    // ── Each equality fails INDIVIDUALLY (zero tolerance, named exception) ───────

    private static PaymentEconomicsSnapshotEntity CreateWith(LineEconomicsInput svc,
        LineEconomicsInput? travel = null, PlatformFeeInput? fee = null, decimal customerPayable = 5800m)
        => PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY",
            travel is null ? new[] { svc } : new[] { svc, travel },
            fee ?? Fee(), customerPayable);

    [Fact]
    public void Tamper_LineCommission_Throws()
    {
        var bad = ServiceLine() with { CommissionAmount = 599m };   // != Round(5000×0.12)=600
        var act = () => CreateWith(bad, TravelLineExempt());
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*TotalProviderCommission*");
    }

    [Fact]
    public void Tamper_LineProviderNet_Throws()
    {
        var bad = ServiceLine() with { ProviderNet = 4399m };       // != 5000 − 600
        var act = () => CreateWith(bad, TravelLineExempt());
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*ProviderNetTotal*");
    }

    [Fact]
    public void Tamper_LineVatGrossReconciliation_Throws()
    {
        var bad = ServiceLine() with { LineVat = 10m };             // (5000−0)+10 != 5000
        var act = () => CreateWith(bad, TravelLineExempt());
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*reconciliation*");
    }

    [Fact]
    public void Tamper_LineTotal_BreaksCustomerTotalAnchor_Throws()
    {
        // lineTotal moved but providerNet/vat kept consistent so the per-line net check still passes,
        // yet ΣlineTotal + feeGross no longer equals CustomerTotal(=customerPayable+feeGross).
        var bad = ServiceLine() with { LineTotal = 5001m, ProviderNet = 4401m };
        var act = () => CreateWith(bad, TravelLineExempt(), customerPayable: 5800m);
        act.Should().Throw<PaymentEconomicsInvariantException>();
    }

    [Fact]
    public void Tamper_PlatformFeeGross_BreaksFeeIdentity_Throws()
    {
        var badFee = Fee(net: 145m, vat: 29m, gross: 175m);        // 175 != 145 + 29
        var act = () => CreateWith(ServiceLine(), TravelLineExempt(), fee: badFee);
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*PlatformFeeGrossAmount*");
    }

    [Fact]
    public void Tamper_CustomerPayable_BreaksTotalAnchor_Throws()
    {
        var act = () => CreateWith(ServiceLine(), TravelLineExempt(), customerPayable: 5700m); // != ΣlineTotal 5800
        act.Should().Throw<PaymentEconomicsInvariantException>().WithMessage("*CustomerTotalAmount*");
    }

    [Fact]
    public void CreateFromLines_EmptyLines_Throws()
    {
        var act = () => PaymentEconomicsSnapshotEntity.CreateFromLines(
            42, "TRY", Array.Empty<LineEconomicsInput>(), Fee(), 0m);
        act.Should().Throw<PaymentEconomicsInvariantException>();
    }

    // ── Determinism: identical inputs → identical aggregates ────────────────────

    [Fact]
    public void CreateFromLines_IsDeterministic()
    {
        var a = CreateSmoke();
        var b = CreateSmoke();

        a.CommissionAmountSnapshot.Should().Be(b.CommissionAmountSnapshot);
        a.ProviderNetAmountSnapshot.Should().Be(b.ProviderNetAmountSnapshot);
        a.CustomerTotalAmountSnapshot.Should().Be(b.CustomerTotalAmountSnapshot);
        a.PlatformGrossShareSnapshot.Should().Be(b.PlatformGrossShareSnapshot);
        a.CommissionRateSnapshot.Should().Be(b.CommissionRateSnapshot);
    }

    // ── Immutability of the line children ───────────────────────────────────────

    [Theory]
    [InlineData(typeof(OfferLineEconomicsSnapshotEntity))]
    [InlineData(typeof(CommissionAllocationSnapshotEntity))]
    [InlineData(typeof(DiscountAllocationSnapshotEntity))]
    public void LineChildren_Expose_No_Public_Setters_Or_Mutators(Type t)
    {
        var settable = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.DeclaringType == t)
            .Select(p => p.Name).ToList();
        settable.Should().BeEmpty("line snapshot children are immutable");

        var mutators = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName).Select(m => m.Name).ToList();
        mutators.Should().BeEmpty("line snapshot children expose no mutator surface");
    }
}
