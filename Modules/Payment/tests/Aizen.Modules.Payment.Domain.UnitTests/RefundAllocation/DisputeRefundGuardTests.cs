using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.RefundAllocation;

/// <summary>
/// BE-S13b — the pure guards behind driving the P10 path from a dispute resolution: refundable-amount validation
/// (partial/split must be positive and ≤ refundable) and the <c>DISPUTE-{id}</c> idempotency anchor (a re-resolve must
/// not issue a second refund).
/// </summary>
public sealed class DisputeRefundGuardTests
{
    private static TransactionRefundRecord Record(string? adminNote, bool failed = false)
    {
        var r = TransactionRefundRecord.Create(
            paymentTransactionId: 1, refundCode: "REF-X", amount: 100m, currencyCode: "TRY",
            refundType: RefundType.Partial, reason: RefundReason.DisputeResolvedForPayer, adminNote: adminNote);
        if (failed) r.MarkFailed("gateway rejected");
        return r;
    }

    [Fact]
    public void Context_ref_is_dispute_prefixed()
        => DisputeRefundGuard.ContextRef(42).Should().Be("DISPUTE-42");

    [Theory] // test (3) — an amount above refundable (or non-positive) is rejected
    [InlineData(0, 1000, false)]
    [InlineData(-1, 1000, false)]
    [InlineData(1500, 1000, false)]   // > refundable → rejected
    [InlineData(1000, 1000, true)]    // == refundable → allowed (full)
    [InlineData(300, 1000, true)]     // < refundable → allowed (partial)
    public void Refundable_amount_is_validated_against_the_ceiling(decimal requested, decimal refundable, bool expected)
        => DisputeRefundGuard.IsRefundableAmountValid(requested, refundable).Should().Be(expected);

    [Fact] // test (2) — a non-Failed record carrying the dispute ref means the refund already ran (idempotent)
    public void Already_applied_is_true_when_a_matching_non_failed_record_exists()
    {
        var records = new[]
        {
            Record("DISPUTE-42: dispute resolved for payer."),
            Record("some other refund"),
        };

        DisputeRefundGuard.AlreadyApplied(records, "DISPUTE-42").Should().BeTrue();
    }

    [Fact] // a Failed record with the ref does NOT count — a later resolve may retry
    public void Failed_records_do_not_count_as_applied()
    {
        var records = new[] { Record("DISPUTE-42: dispute resolved for payer.", failed: true) };

        DisputeRefundGuard.AlreadyApplied(records, "DISPUTE-42").Should().BeFalse();
    }

    [Fact]
    public void Already_applied_is_false_for_a_different_dispute()
    {
        var records = new[] { Record("DISPUTE-99: other dispute.") };

        DisputeRefundGuard.AlreadyApplied(records, "DISPUTE-42").Should().BeFalse();
    }
}
