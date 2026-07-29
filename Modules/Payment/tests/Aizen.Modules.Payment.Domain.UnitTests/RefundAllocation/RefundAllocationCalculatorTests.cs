using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.RefundAllocation;

/// <summary>
/// BE-P10 §7.5 — the pure, snapshot-driven refund allocation. Every reversal is a proportion of the immutable snapshot;
/// the 9 amounts obey the zero-tolerance invariants (ServiceRefund == ProviderNet + Commission; total == service + fee).
/// </summary>
public sealed class RefundAllocationCalculatorTests
{
    // Snapshot: Service 5000 @0.12 → commission 600, providerNet 4400; platform fee net 145 / vat 29 / gross 174.
    private static PaymentEconomicsSnapshotEntity Snapshot(decimal serviceGross = 5000m)
    {
        var line = new LineEconomicsInput(
            LineRef: "L1", ItemType: 1, PricingMethod: 1,
            GrossBeforeDiscount: serviceGross, CustomerDiscount: 0m, ProviderFundedDiscount: 0m, PlatformFundedDiscount: 0m,
            CommissionEligibility: LineCommissionEligibility.Eligible,
            CommissionBase: serviceGross, CommissionRate: 0.12m, CommissionAmount: serviceGross * 0.12m,
            ProviderNet: serviceGross - serviceGross * 0.12m, LineVat: 0m, LineTotal: serviceGross,
            RuleId: null, RuleCode: "STD", Commissionable: true);
        var fee = new PlatformFeeInput(RuleId: 3, Rate: 0.025m, Minimum: 99m, Maximum: 1500m, Base: serviceGross, Net: 145m, Vat: 29m, Gross: 174m);
        return PaymentEconomicsSnapshotEntity.CreateFromLines(42, "TRY", new[] { line }, fee, serviceGross);
    }

    // ── Full provider-cancel refund: reversals sum to the snapshot; total == service + fee ──

    [Fact]
    public void FullRefund_ProviderCancel_FeeFull_SumsToSnapshot()
    {
        var s = Snapshot();
        var a = RefundAllocationCalculator.Resolve(s, refundServiceAmount: 5000m,
            RefundCause.ProviderCancelled, ReleaseState.AfterProviderRelease, PlatformFeeRefundMode.Full);

        a.ServiceRefundAmount.Should().Be(5000m);
        a.CommissionRevenueReversalAmount.Should().Be(600m);
        a.ProviderNetReversalAmount.Should().Be(4400m);
        a.PlatformFeeNetRefundAmount.Should().Be(145m);
        a.PlatformFeeVatRefundAmount.Should().Be(29m);
        a.PlatformFeeGrossRefundAmount.Should().Be(174m);
        a.TotalRefundToGateway.Should().Be(5174m);                 // service 5000 + fee gross 174
        a.ServiceRefundAmount.Should().Be(a.ProviderNetReversalAmount + a.CommissionRevenueReversalAmount);   // §7.5
        a.ProviderRecoveryAmount.Should().Be(4400m);               // release-after → recover the provider net
    }

    // ── Partial pro-rata: half the service ──

    [Fact]
    public void PartialRefund_ProRata_Half()
    {
        var s = Snapshot();
        var a = RefundAllocationCalculator.Resolve(s, refundServiceAmount: 2500m,
            RefundCause.CustomerCancelledAfterWorkStarted, ReleaseState.BeforeProviderRelease, PlatformFeeRefundMode.ProRata);

        a.CommissionRevenueReversalAmount.Should().Be(300m);       // 600 × 0.5
        a.ProviderNetReversalAmount.Should().Be(2200m);            // 2500 − 300
        a.PlatformFeeNetRefundAmount.Should().Be(72.5m);           // 145 × 0.5
        a.PlatformFeeVatRefundAmount.Should().Be(14.5m);           // 29 × 0.5
        a.PlatformFeeGrossRefundAmount.Should().Be(87m);
        a.TotalRefundToGateway.Should().Be(2587m);                 // 2500 + 87
        a.ProviderRecoveryAmount.Should().Be(0m);                  // release-before → no recovery
    }

    // ── Fee modes ───────────────────────────────────────────────────────────────

    [Fact]
    public void FeeMode_None_KeepsPlatformFee()
    {
        var a = RefundAllocationCalculator.Resolve(Snapshot(), 5000m,
            RefundCause.DisputeProviderFavoured, ReleaseState.BeforeProviderRelease, PlatformFeeRefundMode.None);
        a.PlatformFeeGrossRefundAmount.Should().Be(0m);
        a.TotalRefundToGateway.Should().Be(5000m);                 // only the service is refunded
    }

    // ── Snapshot-truth: reversals rounded from the snapshot sum exactly (odd amount) ──

    [Fact]
    public void SnapshotTruth_RoundingSumsExactly()
    {
        var s = Snapshot(3333.33m);   // commission 399.9996 → snapshot rounds to 400.00; net 2933.33
        var a = RefundAllocationCalculator.Resolve(s, 3333.33m,
            RefundCause.ProviderCancelled, ReleaseState.AfterProviderRelease, PlatformFeeRefundMode.Full);
        (a.ProviderNetReversalAmount + a.CommissionRevenueReversalAmount).Should().Be(a.ServiceRefundAmount);
    }

    // ── Mismatch guard (zero tolerance) ─────────────────────────────────────────

    [Fact]
    public void Verify_TamperedBreakdown_Throws()
    {
        var bad = new Aizen.Modules.Payment.Domain.Entities.RefundAllocation.RefundAllocation(
            ServiceRefundAmount: 1000m, ProviderNetReversalAmount: 700m, CommissionRevenueReversalAmount: 250m,  // 700+250 != 1000
            PlatformFeeNetRefundAmount: 0m, PlatformFeeVatRefundAmount: 0m, PlatformFeeGrossRefundAmount: 0m,
            GatewayRefundExpenseAmount: 0m, ProviderRecoveryAmount: 0m, PlatformAdvancedRefundAmount: 0m,
            RemainingProviderNegativeBalance: 0m);

        var act = () => RefundAllocationCalculator.Verify(bad);
        act.Should().Throw<AizenBusinessException>().WithMessage("*ServiceRefund*");
    }

    [Fact]
    public void Resolve_RefundExceedsService_Throws()
    {
        var act = () => RefundAllocationCalculator.Resolve(Snapshot(), refundServiceAmount: 6000m,
            RefundCause.ProviderCancelled, ReleaseState.BeforeProviderRelease, PlatformFeeRefundMode.Full);
        act.Should().Throw<AizenBusinessException>();
    }
}
