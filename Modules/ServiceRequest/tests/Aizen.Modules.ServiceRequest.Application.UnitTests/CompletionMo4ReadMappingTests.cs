using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE_MO4 — the owner completion-review read surface. The mobile owner reads the provider's completion (via the SR
/// detail, which carries Completion.ToDto()), so the additive read fields must survive the entity → DTO mapping:
/// the N3 <c>AutoApproveAt</c> that drives the owner's countdown, the optional <c>ClientRating</c> captured at
/// approval, and the N-E <c>RejectReasonCode</c> surfaced after a reject. An owner action before the deadline moves
/// the completion out of Submitted (the N3 guard that cancels the still-pending auto-approval) — MO4 only reads;
/// the approve/reject commands + the decoupled escrow release are unchanged.
/// </summary>
public sealed class CompletionMo4ReadMappingTests
{
    private static ServiceRequestCompletionEntity NewSubmitted() =>
        ServiceRequestCompletionEntity.Create(
            serviceRequestId: 101, serviceRequestAssignmentId: 5, providerUserId: 88,
            completionNotes: "sensor recalibrated", evidenceFileId: Guid.NewGuid());

    [Fact] // (4) the countdown source: a scheduled deadline is exposed verbatim on the read DTO
    public void ToDto_exposes_the_auto_approve_deadline_for_the_countdown()
    {
        var c = NewSubmitted();
        var deadline = c.SubmittedAt.AddDays(7);
        c.ScheduleAutoApproval(deadline);
        c.MarkAutoApproveReminderSent();

        var dto = c.ToDto();

        dto.AutoApproveAt.Should().Be(deadline);
        dto.AutoApproveReminderSentAt.Should().NotBeNull();
        dto.Status.Should().Be(ServiceRequestCompletionStatus.Submitted);
    }

    [Fact] // a pre-N3-C row (never scheduled) → no deadline, so the FE hides the countdown
    public void ToDto_auto_approve_is_null_when_never_scheduled()
    {
        var c = NewSubmitted();

        var dto = c.ToDto();

        dto.AutoApproveAt.Should().BeNull();
        dto.AutoApproveReminderSentAt.Should().BeNull();
    }

    [Fact] // (2) approve with an optional rating → the rating rides the read DTO; approve reuses the same path
    public void ToDto_exposes_the_client_rating_captured_at_approval()
    {
        var c = NewSubmitted();
        c.ScheduleAutoApproval(c.SubmittedAt.AddDays(7));

        c.ApproveByOwner(reviewerUserId: 7, reviewNotes: "great work");
        c.RateByClient(5);

        var dto = c.ToDto();

        dto.Status.Should().Be(ServiceRequestCompletionStatus.ApprovedByOwner);
        dto.ClientRating.Should().Be(5);
        dto.ReviewNotes.Should().Be("great work");
        // The frozen deadline survives; the status guard (not clearing the deadline) is what cancels auto-approval.
        dto.AutoApproveAt.Should().NotBeNull();
    }

    [Fact] // (3) reject with an N-E reason → the structured reason is surfaced on the read DTO
    public void ToDto_surfaces_the_reject_reason()
    {
        var c = NewSubmitted();

        c.RejectByOwner(reviewerUserId: 7, reviewNotes: "left oil leak", reasonCode: CompletionRejectReason.QualityIssue);

        var dto = c.ToDto();

        dto.Status.Should().Be(ServiceRequestCompletionStatus.RejectedByOwner);
        dto.RejectReasonCode.Should().Be(CompletionRejectReason.QualityIssue);
        dto.ReviewNotes.Should().Be("left oil leak");
    }

    [Theory] // (6) an owner action before the deadline moves out of Submitted → the pending auto-approval is cancelled
    [InlineData("approve")]
    [InlineData("reject")]
    public void Owner_action_before_deadline_cancels_the_pending_auto_approval(string action)
    {
        var c = NewSubmitted();
        c.ScheduleAutoApproval(c.SubmittedAt.AddDays(7));

        if (action == "approve") c.ApproveByOwner(7, "ok");
        else c.RejectByOwner(7, "no", CompletionRejectReason.WorkIncomplete);

        c.ToDto().Status.Should().NotBe(ServiceRequestCompletionStatus.Submitted);
    }

    [Fact] // the read stays cost-free: the DTO carries no cost/commission/net figures — only descriptive review fields
    public void ToDto_carries_no_cost_or_commission_fields()
    {
        var props = typeof(Aizen.Modules.ServiceRequest.Abstraction.Dto.ServiceRequestCompletionDto)
            .GetProperties().Select(p => p.Name);

        props.Should().NotContain(n =>
            n.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Commission", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Margin", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Funding", StringComparison.OrdinalIgnoreCase));
    }
}
