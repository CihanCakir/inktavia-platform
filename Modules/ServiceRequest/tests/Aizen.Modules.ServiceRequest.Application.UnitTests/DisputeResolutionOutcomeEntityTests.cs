using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S13b/c — the dispute entity's resolution-outcome semantics: notes-only resolve is unchanged (no monetary
/// outcome), and the monetary outcome is stamped exactly once (the idempotency anchor a re-resolve relies on). Also
/// asserts the N3 resolved-message payload (both parties + outcome).
/// </summary>
public sealed class DisputeResolutionOutcomeEntityTests
{
    private static ServiceRequestDisputeEntity NewDispute() =>
        ServiceRequestDisputeEntity.Create(
            serviceRequestId: 42,
            openedByUserId: 7,
            openedByActorType: ServiceRequestActorType.Owner,
            reason: ServiceRequestDisputeReason.QualityIssue,
            description: "Work not as agreed");

    [Fact] // test (5) — notes-only resolve is unchanged: no monetary outcome, nothing applied to Payment
    public void Notes_only_resolve_records_no_monetary_outcome()
    {
        var dispute = NewDispute();

        dispute.Resolve(adminUserId: 99, resolutionNotes: "Talked to both parties");

        dispute.Status.Should().Be(ServiceRequestDisputeStatus.Resolved);
        dispute.ResolutionNotes.Should().Be("Talked to both parties");
        dispute.ResolvedByAdminUserId.Should().Be(99);
        dispute.ResolutionOutcome.Should().BeNull();
        dispute.ResolutionRefundAmount.Should().BeNull();
        dispute.IsPaymentOutcomeApplied.Should().BeFalse();
    }

    [Fact] // test (2) — the monetary outcome is idempotent: a re-resolve never re-applies (no double refund)
    public void Payment_outcome_is_stamped_exactly_once()
    {
        var dispute = NewDispute();
        dispute.Resolve(adminUserId: 99, resolutionNotes: null);

        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerFullRefund, refundAmount: 100m);

        dispute.IsPaymentOutcomeApplied.Should().BeTrue();
        dispute.ResolutionOutcome.Should().Be(DisputeResolutionOutcome.FavorPayerFullRefund);
        dispute.ResolutionRefundAmount.Should().Be(100m);
        var appliedAt = dispute.PaymentOutcomeAppliedAt;

        // A second (re-)resolve must NOT overwrite the applied outcome — the double-refund guard.
        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerPartialRefund, refundAmount: 50m);

        dispute.ResolutionOutcome.Should().Be(DisputeResolutionOutcome.FavorPayerFullRefund);
        dispute.ResolutionRefundAmount.Should().Be(100m);
        dispute.PaymentOutcomeAppliedAt.Should().Be(appliedAt);
    }

    [Fact] // test (6) — the resolved bus message carries both parties + the outcome for N3
    public void Resolved_message_carries_both_parties_and_outcome()
    {
        var dispute = NewDispute();
        dispute.Resolve(adminUserId: 99, resolutionNotes: null);
        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerPartialRefund, refundAmount: 75m);

        var message = DisputeResolvedMessageFactory.Build(dispute, ownerUserId: 7, providerUserId: 88);

        message.ServiceRequestId.Should().Be(42);
        message.OwnerUserId.Should().Be(7);
        message.ProviderUserId.Should().Be(88);
        message.ResolvedByAdminUserId.Should().Be(99);
        message.Outcome.Should().Be((int)DisputeResolutionOutcome.FavorPayerPartialRefund);
        message.RefundAmount.Should().Be(75m);
        message.Reason.Should().Be(ServiceRequestDisputeReason.QualityIssue);
    }

    [Fact] // notes-only resolved message → outcome 0, refund 0
    public void Notes_only_resolved_message_has_zero_outcome()
    {
        var dispute = NewDispute();
        dispute.Resolve(adminUserId: 99, resolutionNotes: "no money");

        var message = DisputeResolvedMessageFactory.Build(dispute, ownerUserId: 7, providerUserId: 88);

        message.Outcome.Should().Be(0);
        message.RefundAmount.Should().Be(0m);
    }
}
