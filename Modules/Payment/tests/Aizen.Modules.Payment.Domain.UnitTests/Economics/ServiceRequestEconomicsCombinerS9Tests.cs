using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

/// <summary>
/// BE-S9 (§20.12) — line-level profit protection wired into the P8 combiner. The line gate runs on the FINAL committed
/// economics: each line must independently clear its own floor (NO netting) or the offer is Rejected / ConfigurationError
/// with no snapshot. A passing offer's amounts + 8-equality are byte-identical to pre-S9 (S9 only adds a rejection path
/// and a descriptive per-line record).
/// </summary>
public sealed class ServiceRequestEconomicsCombinerS9Tests
{
    private static readonly DateTime At = new(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc);

    private static ServiceRequestEconomicsLine ServiceLine(
        string @ref = "L-SVC", decimal gross = 5000m, decimal rate = 0.12m, decimal commission = 600m, bool commissionable = true, bool part = false, int itemType = 1)
        => new(@ref, ItemType: itemType, PricingMethod: 1, LineCommissionEligibility.Eligible,
            LineGrossBeforeDiscount: gross, LineVat: 0m,
            Commissionable: commissionable, CommissionBase: commissionable ? gross : 0m, ResolvedRate: rate,
            CommissionAmount: commission, ProviderNet: gross - commission, RuleCode: commissionable ? "PLAN-STD" : null,
            IsPartLine: part);

    private static ServiceRequestEconomicsLine TravelExempt(decimal gross = 800m)
        => new("L-TRV", ItemType: 10, PricingMethod: 1, LineCommissionEligibility.Exempt,
            LineGrossBeforeDiscount: gross, LineVat: 0m,
            Commissionable: false, CommissionBase: 0m, ResolvedRate: 0m, CommissionAmount: 0m,
            ProviderNet: gross, RuleCode: null);

    private static PlatformFeeResolution FeeRule()
        => new(RuleId: 3, RuleCode: "PF-STD", Model: PlatformFeeModel.PercentageWithBounds,
            Rate: 0.025m, MinAmount: 99m, MaxAmount: 1500m, FixedAmount: null, VatRate: 0.20m,
            SpecificityRank: 1, Priority: CommissionRulePriority.Standard, Source: "Global");

    private static PlatformFeeBreakdown Fee() => new(145m, 29m, 174m, VatRate: 0.20m, VatSource: "Rule");

    private static ProfitProtectionPolicyEntity Policy(decimal minLineContribRate = 0m)
    {
        var p = ProfitProtectionPolicyEntity.Create(
            "TRY", 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "PPOL-1", null, null,
            // line-level: caps 100% (no-op), configurable min line-contribution rate
            0m, 0m, 1m, 1m, 0m, minLineContribRate, false, 0m);
        p.Id = 1;
        return p;
    }

    private static ServiceRequestEconomicsResult Combine(
        IReadOnlyList<ServiceRequestEconomicsLine>? lines = null,
        ProfitProtectionPolicyEntity? policy = null,
        IReadOnlyDictionary<string, LinePartAllowance>? partAllowances = null)
        => ServiceRequestEconomicsCombiner.Combine(
            serviceRequestId: 42, currencyCode: "TRY",
            lines: lines ?? new[] { ServiceLine(), TravelExempt() },
            platformFeeRule: FeeRule(), platformFee: Fee(),
            policy: policy ?? Policy(), createdAtUtc: At, customerDiscount: null,
            partAllowances: partAllowances);

    // ── (1) Regression: all lines clear → Approved; amounts + 8-equality byte-identical to pre-S9; the S9 record is added ─
    [Fact]
    public void AllLinesClear_Approved_AmountsUnchanged_RecordAdded()
    {
        var r = Combine();

        r.Decision.Should().Be(ProfitProtectionDecisionState.Approved);
        // pre-S9 amounts, unchanged
        r.OriginalServiceGrossAmount.Should().Be(5800m);
        r.TransactionCommission.Should().Be(600m);
        r.ProviderNetTotal.Should().Be(5200m);
        r.CustomerTotalAmount.Should().Be(5974m);

        var s = r.Snapshot!;
        // 8-equality structural checks still hold
        (s.OfferLines.Sum(l => l.LineTotalAmount) + s.PlatformFeeGrossAmountSnapshot)
            .Should().Be(s.CustomerTotalAmountSnapshot);
        (s.ProviderNetAmountSnapshot + s.PlatformGrossShareSnapshot).Should().Be(s.CustomerTotalAmountSnapshot);

        // S9 descriptive record populated on the passing offer (enters no sum/invariant)
        r.LineProtectionResults.Should().NotBeNull();
        r.LineProtectionResults!.Should().OnlyContain(x => x.Passed);
        var svc = s.OfferLines.Single(l => l.LineRef == "L-SVC");
        svc.LineProfitProtectionPassed.Should().BeTrue();
        svc.LinePlatformContribution.Should().Be(745m);   // commission 600 + proRata fee 145 (single commissionable line)
        s.OfferLines.Single(l => l.LineRef == "L-TRV").LinePlatformContribution.Should().Be(0m);
    }

    // ── (2) A PART line priced below its S5 MinimumProviderReceivable → Rejected, no snapshot, per-line reason ──
    [Fact]
    public void PartLine_BelowS5MinReceivable_Rejected_NoSnapshot()
    {
        var part = ServiceLine(@ref: "L-PART", gross: 1000m, rate: 0.12m, commission: 120m, part: true, itemType: 2);
        var allowances = new Dictionary<string, LinePartAllowance>
        {
            ["L-PART"] = new LinePartAllowance(Found: true, MinimumProviderReceivable: 1000m,
                AllowedProviderFundedDiscount: 1000m, AllowedPlatformFundedDiscount: 1000m),
        };
        var r = Combine(new[] { part }, partAllowances: allowances);   // ProviderNet 880 < 1000

        r.Decision.Should().Be(ProfitProtectionDecisionState.Rejected);
        r.Snapshot.Should().BeNull();
        r.CanProceed.Should().BeFalse();
        r.LineProtectionFailed.Should().BeTrue();
        r.LineProtectionErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionProviderReceivableBelowFloor);
        r.Reason.Should().Contain("L-PART");
    }

    // ── (5) NETTING — a profitable line + a line below its own floor that net-pass at the transaction level → Rejected ──
    [Fact]
    public void Netting_TransactionAggregatePasses_ButLineFails_Rejected()
    {
        // Line A commissionable (contribution 600+fee); Line B exempt (contribution 0). Require each line contribute
        // ≥ gross × 0.05 = 250. The transaction aggregate (600+fee) clears, but line B (0) is below its own floor.
        var a = ServiceLine(@ref: "L-A", gross: 5000m, commission: 600m, commissionable: true);
        var b = ServiceLine(@ref: "L-B", gross: 5000m, commission: 0m, commissionable: false);
        var r = Combine(new[] { a, b }, policy: Policy(minLineContribRate: 0.05m));

        r.Decision.Should().Be(ProfitProtectionDecisionState.Rejected);   // the +600 line does NOT rescue line B
        r.Snapshot.Should().BeNull();
        r.LineProtectionFailed.Should().BeTrue();
        r.LineProtectionErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionNegativeContribution);
        r.LineProtectionResults!.Single(x => x.LineRef == "L-A").Passed.Should().BeTrue();
        r.LineProtectionResults!.Single(x => x.LineRef == "L-B").Passed.Should().BeFalse();
    }

    // ── (7) A part line with NO resolved S5 allowance → ConfigurationError, no snapshot ──
    [Fact]
    public void PartLine_MissingAllowance_ConfigurationError_NoSnapshot()
    {
        var part = ServiceLine(@ref: "L-PART", gross: 1000m, commission: 120m, part: true, itemType: 2);
        var r = Combine(new[] { part }, partAllowances: new Dictionary<string, LinePartAllowance>());

        r.Decision.Should().Be(ProfitProtectionDecisionState.ConfigurationError);
        r.Snapshot.Should().BeNull();
        r.LineProtectionFailed.Should().BeTrue();
        r.LineProtectionErrorCode.Should().Be((int)PaymentErrorCode.LineProfitProtectionConfigurationError);
    }
}
