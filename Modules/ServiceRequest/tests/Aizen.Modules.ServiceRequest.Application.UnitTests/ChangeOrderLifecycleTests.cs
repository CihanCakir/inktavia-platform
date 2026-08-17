using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S11b — the change-order domain lifecycle + the DERIVED effective total. Proves: a proposal is financially inert; an
/// applied Increase adds a positive delta, an applied Decrease a negative one; illegal transitions throw; and the SR
/// effective total = original + Σ applied deltas (never stored on the original acceptance snapshot).
/// </summary>
public sealed class ChangeOrderLifecycleTests
{
    private static ServiceChangeOrderEntity NewCo(ServiceChangeOrderDirection direction = ServiceChangeOrderDirection.Increase)
        => ServiceChangeOrderEntity.Create(
            serviceRequestId: 10, acceptedOfferId: 20, sequenceNo: 1, direction: direction,
            currencyCode: "TRY", reason: "extra work", proposedByUserId: 99,
            items: new[] { ServiceChangeOrderItemEntity.Create(
                ServiceRequestOfferItemType.Labor, "Labor", null, 2, 500m, "TRY", 0) },
            utcNow: DateTime.UtcNow);

    // ── (2) Propose → nothing financial: Proposed, zero delta, no snapshot/escrow ──
    [Fact]
    public void Proposed_ChangeOrder_IsFinanciallyInert()
    {
        var co = NewCo();
        co.Status.Should().Be(ServiceChangeOrderStatus.Proposed);
        co.EffectiveTotalDelta.Should().Be(0m, "a proposal is not in any total/collection until approved+applied");
        co.EconomicsSnapshotId.Should().BeNull();
        co.PaymentTransactionId.Should().BeNull();
        co.Items.Should().ContainSingle();
    }

    // ── (3) Approve → apply (Increase): positive delta + snapshot/escrow links; original untouched (delta is derived) ──
    [Fact]
    public void ApprovedThenApplied_Increase_AddsPositiveDelta()
    {
        var co = NewCo(ServiceChangeOrderDirection.Increase);
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.Status.Should().Be(ServiceChangeOrderStatus.CustomerApproved);

        co.MarkApplied(economicsSnapshotId: 555, paymentTransactionId: 777, customerTotal: 1200m, providerNet: 1000m, utcNow: DateTime.UtcNow);

        co.Status.Should().Be(ServiceChangeOrderStatus.Applied);
        co.EconomicsSnapshotId.Should().Be(555);
        co.PaymentTransactionId.Should().Be(777);
        co.AppliedCustomerTotal.Should().Be(1200m);
        co.EffectiveTotalDelta.Should().Be(1200m, "an applied increase adds to the effective total");
    }

    // ── (6) Reduction (Decrease) → negative delta via the P10 link ──
    [Fact]
    public void ApprovedThenApplied_Decrease_AddsNegativeDelta()
    {
        var co = NewCo(ServiceChangeOrderDirection.Decrease);
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.MarkAppliedAsReduction(originalTransactionId: 777, refundRecordId: 888, refundedAmount: 400m, utcNow: DateTime.UtcNow);

        co.Status.Should().Be(ServiceChangeOrderStatus.Applied);
        co.RefundRecordId.Should().Be(888);
        co.PaymentTransactionId.Should().Be(777, "a reduction links the ORIGINAL transaction it refunded");
        co.EconomicsSnapshotId.Should().BeNull("a reduction produces no new snapshot");
        co.EffectiveTotalDelta.Should().Be(-400m);
    }

    // ── (5) Breach → Rejected (from CustomerApproved), no snapshot/collection, zero delta ──
    [Fact]
    public void Approved_ThenRejectedOnBreach_LeavesNoEconomics()
    {
        var co = NewCo();
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.Reject("Incremental economics Rejected: profit protection breach", DateTime.UtcNow);

        co.Status.Should().Be(ServiceChangeOrderStatus.Rejected);
        co.EffectiveTotalDelta.Should().Be(0m);
        co.EconomicsSnapshotId.Should().BeNull();
        co.PaymentTransactionId.Should().BeNull();
    }

    // ── (4) Idempotency guard: an applied order cannot be re-approved or re-applied ──
    [Fact]
    public void AppliedChangeOrder_CannotBeReApproved_OrReApplied()
    {
        var co = NewCo();
        co.MarkCustomerApproved(DateTime.UtcNow);
        co.MarkApplied(1, 2, 100m, 90m, DateTime.UtcNow);

        co.Invoking(c => c.MarkCustomerApproved(DateTime.UtcNow)).Should().Throw<InvalidOperationException>();
        co.Invoking(c => c.MarkApplied(9, 9, 9m, 9m, DateTime.UtcNow)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CustomerCanReject_AProposal_Terminal_NoEconomics()
    {
        var co = NewCo();
        co.Reject("chose not to proceed", DateTime.UtcNow);
        co.Status.Should().Be(ServiceChangeOrderStatus.Rejected);
        co.Invoking(c => c.MarkCustomerApproved(DateTime.UtcNow)).Should().Throw<InvalidOperationException>();
    }

    // ── (3, derived) SR effective total = original acceptance + Σ applied deltas ──
    [Fact]
    public void EffectiveTotal_IsOriginalPlusSumOfAppliedDeltas()
    {
        const decimal originalAcceptanceTotal = 5000m;   // the accepted offer's grand total (never mutated)

        var inc = NewCo(ServiceChangeOrderDirection.Increase);
        inc.MarkCustomerApproved(DateTime.UtcNow);
        inc.MarkApplied(1, 1, 1200m, 1000m, DateTime.UtcNow);

        var dec = NewCo(ServiceChangeOrderDirection.Decrease);
        dec.MarkCustomerApproved(DateTime.UtcNow);
        dec.MarkAppliedAsReduction(1, 2, 300m, DateTime.UtcNow);

        var proposedNotApplied = NewCo();   // still Proposed — must NOT count

        var appliedDelta = new[] { inc, dec, proposedNotApplied }
            .Where(c => c.Status == ServiceChangeOrderStatus.Applied)
            .Sum(c => c.EffectiveTotalDelta);

        appliedDelta.Should().Be(900m, "+1200 increase −300 reduction; the un-applied proposal is excluded");
        (originalAcceptanceTotal + appliedDelta).Should().Be(5900m);
    }
}
