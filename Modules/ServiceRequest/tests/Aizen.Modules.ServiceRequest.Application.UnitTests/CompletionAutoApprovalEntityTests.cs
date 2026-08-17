using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// N3-C — the completion entity's auto-approval semantics: the deadline is frozen at submission, the reminder is a
/// once-guard, and any owner action (approve/reject/dispute) moves the status out of Submitted so the job's
/// still-pending query (and the approval handler's guard) cancels the auto-approval.
/// </summary>
public sealed class CompletionAutoApprovalEntityTests
{
    private static ServiceRequestCompletionEntity NewSubmitted() =>
        ServiceRequestCompletionEntity.Create(
            serviceRequestId: 1, serviceRequestAssignmentId: 2, providerUserId: 88,
            completionNotes: "done", evidenceFileId: Guid.NewGuid());

    [Fact]
    public void New_completion_is_submitted_with_no_deadline_until_scheduled()
    {
        var c = NewSubmitted();
        c.Status.Should().Be(ServiceRequestCompletionStatus.Submitted);
        c.AutoApproveAt.Should().BeNull();
        c.AutoApproveReminderSentAt.Should().BeNull();
    }

    [Fact]
    public void ScheduleAutoApproval_freezes_the_deadline()
    {
        var c = NewSubmitted();
        var deadline = c.SubmittedAt.AddDays(7);

        c.ScheduleAutoApproval(deadline);

        c.AutoApproveAt.Should().Be(deadline);
    }

    [Fact]
    public void Reminder_is_stamped_once()
    {
        var c = NewSubmitted();
        c.AutoApproveReminderSentAt.Should().BeNull();

        c.MarkAutoApproveReminderSent();

        c.AutoApproveReminderSentAt.Should().NotBeNull();
    }

    [Theory] // an owner action before the deadline moves out of Submitted → auto-approval no longer applies
    [InlineData("approve")]
    [InlineData("reject")]
    [InlineData("dispute")]
    public void Owner_action_moves_out_of_submitted(string action)
    {
        var c = NewSubmitted();
        c.ScheduleAutoApproval(c.SubmittedAt.AddDays(7));

        switch (action)
        {
            case "approve": c.ApproveByOwner(7, "ok"); break;
            case "reject":  c.RejectByOwner(7, "no", CompletionRejectReason.WorkIncomplete); break;
            case "dispute": c.DisputeByOwner(7, "contested"); break;
        }

        c.Status.Should().NotBe(ServiceRequestCompletionStatus.Submitted,
            "an owner action cancels the pending auto-approval");
    }
}
