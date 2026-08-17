using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.RefundAllocation;

/// <summary>BE-P10 §7.4/§7.1/§19.15 — provider negative-balance ledger, refund-allocation policy resolve, restore-once.</summary>
public sealed class ProviderBalanceAndPolicyTests
{
    private static readonly DateTime At = new(2026, 7, 28, 9, 0, 0, DateTimeKind.Utc);

    // ── §7.4 negative-balance ledger: clawback → negative; payout offsets first; limit guard ──

    [Fact]
    public void Clawback_GoesNegative_PayoutOffsetsFirst()
    {
        var b = ProviderBalanceEntity.Create(providerProfileId: 7, "TRY", negativeBalanceLimit: 10000m);
        b.Clawback(4400m, ProviderBalanceMovementType.RefundClawback, refundRecordId: 1, chargebackRecordId: null, note: "refund", At);

        b.Balance.Should().Be(-4400m);
        b.NegativeAmount.Should().Be(4400m);
        b.Movements.Should().ContainSingle();
        b.Movements.Single().BalanceAfter.Should().Be(-4400m);

        // A 3000 payout offsets the negative balance first (partial), leaving −1400.
        var applied = b.OffsetFromPayout(3000m, At);
        applied.Should().Be(3000m);
        b.Balance.Should().Be(-1400m);

        // A 5000 payout offsets only the remaining 1400.
        b.OffsetFromPayout(5000m, At).Should().Be(1400m);
        b.Balance.Should().Be(0m);
        b.OffsetFromPayout(5000m, At).Should().Be(0m);   // nothing to offset when non-negative
    }

    [Fact]
    public void NegativeBalanceLimit_Guard()
    {
        var b = ProviderBalanceEntity.Create(7, "TRY", negativeBalanceLimit: 1000m);
        b.Clawback(800m, ProviderBalanceMovementType.RefundClawback, 1, null, null, At);
        b.IsOverLimit().Should().BeFalse();                // 800 ≤ 1000
        b.Clawback(500m, ProviderBalanceMovementType.ChargebackClawback, null, 2, null, At);
        b.IsOverLimit().Should().BeTrue();                 // 1300 > 1000 → blocks payout/acceptance
    }

    [Fact]
    public void ManualAdjust_Audited()
    {
        var b = ProviderBalanceEntity.Create(7, "TRY", 0m);
        b.Clawback(500m, ProviderBalanceMovementType.RefundClawback, 1, null, null, At);
        b.ManualAdjust(500m, adminUserId: 99, "goodwill", At);
        b.Balance.Should().Be(0m);
        b.Movements.Last().MovementType.Should().Be(ProviderBalanceMovementType.ManualAdjustment);
        b.Movements.Last().AdminUserId.Should().Be(99);
    }

    // ── §7.1 policy resolve + per-cause rule ────────────────────────────────────

    [Fact]
    public void Policy_Resolves_SingleActive_And_PerCauseRule()
    {
        var p = RefundAllocationPolicyEntity.Create("TRY", negativeBalanceLimit: 5000m, effectiveFrom: At.AddYears(-1), effectiveTo: null, policyCode: "RAP-1");
        p.AddRule(RefundCause.ProviderCancelled, PlatformFeeRefundMode.Full)
         .AddRule(RefundCause.DisputeProviderFavoured, PlatformFeeRefundMode.None);

        RefundAllocationPolicyResolver.Resolve(new[] { p }, "TRY", At).Should().Be(p);
        p.RuleFor(RefundCause.ProviderCancelled).Mode.Should().Be(PlatformFeeRefundMode.Full);
        p.RuleFor(RefundCause.DisputeProviderFavoured).Mode.Should().Be(PlatformFeeRefundMode.None);
        p.RuleFor(RefundCause.TechnicalFailure).Mode.Should().Be(PlatformFeeRefundMode.Full);   // default when no rule
    }

    [Fact]
    public void Policy_Conflict_TwoActive_Throws()
    {
        var a = RefundAllocationPolicyEntity.Create("TRY", 0m, At.AddYears(-1), null, "A");
        var b = RefundAllocationPolicyEntity.Create("TRY", 0m, At.AddYears(-1), null, "B");
        var act = () => RefundAllocationPolicyResolver.Resolve(new[] { a, b }, "TRY", At);
        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void CauseMap_ReconcilesReason()
    {
        RefundCauseMap.FromReason(RefundReason.DuplicateCharge).Should().Be(RefundCause.DuplicatePayment);
        RefundCauseMap.FromReason(RefundReason.ProviderFailedToDeliver).Should().Be(RefundCause.ProviderCancelled);
        RefundCauseMap.FromReason(RefundReason.DisputeResolvedForPayer).Should().Be(RefundCause.DisputeCustomerFavoured);
        RefundCauseMap.FromReason(RefundReason.PriceAdjustment).Should().Be(RefundCause.AdministrativeCorrection);
    }

    // ── §19.15 benefit restore ONCE (idempotent per refund) ─────────────────────

    [Fact]
    public void BenefitRestore_AppliesOnce_DuplicateThrows()
    {
        var record = TransactionRefundRecord.Create(1, "REF-1", 100m, "TRY", RefundType.Full, RefundReason.UserCancel);
        record.BenefitRestoreApplied.Should().BeFalse();
        record.MarkBenefitRestoreApplied();
        record.BenefitRestoreApplied.Should().BeTrue();

        var act = () => record.MarkBenefitRestoreApplied();   // duplicate refund/webhook
        act.Should().Throw<AizenBusinessException>();
    }

    // ── Chargeback record idempotency signal ────────────────────────────────────

    [Fact]
    public void Chargeback_RecordsExpense_AndRecovery()
    {
        var c = ChargebackRecordEntity.Create(paymentTransactionId: 1, gatewayChargebackReference: "CB-abc",
            amount: 5174m, "TRY", chargebackExpenseAmount: 50m, receivedAtUtc: At);
        c.SetRecovery(recovered: 4400m, remainingNegative: 774m);
        c.ChargebackExpenseAmount.Should().Be(50m);
        c.ProviderRecoveredAmount.Should().Be(4400m);
        c.RemainingNegativeBalance.Should().Be(774m);
        c.GatewayChargebackReference.Should().Be("CB-abc");
    }
}
