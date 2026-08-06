using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// S12/N2 — the maintenance schedule's domain semantics: NextDueAt = (LastPerformedAt ?? created-anchor) + interval;
/// RecordPerformed advances the cycle and re-arms the reminder; the reminder window opens at NextDueAt − lead and is a
/// once-guard. Pure/UTC — no DB.
/// </summary>
public sealed class MaintenanceScheduleEntityTests
{
    private static MaintenanceScheduleEntity NewSchedule(
        int intervalMonths = 12, int leadDays = 14, DateTime? lastPerformedAt = null) =>
        MaintenanceScheduleEntity.Create(
            vesselId: 10, serviceCategoryCode: "ENGINE", serviceTypeCode: null, ownerUserId: 100,
            recommendedIntervalMonths: intervalMonths, reminderLeadDays: leadDays,
            lastPerformedAt: lastPerformedAt);

    // (1) create schedule → NextDueAt from interval.
    [Fact]
    public void Create_from_last_performed_sets_NextDueAt_from_the_interval()
    {
        var last = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var s = NewSchedule(intervalMonths: 12, lastPerformedAt: last);

        s.LastPerformedAt.Should().Be(last);
        s.NextDueAt.Should().Be(last.AddMonths(12)); // 2027-01-15
        s.ReminderSentAt.Should().BeNull();
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_without_last_performed_anchors_NextDueAt_on_now()
    {
        var before = DateTime.UtcNow;
        var s = NewSchedule(intervalMonths: 6);
        var after = DateTime.UtcNow;

        s.LastPerformedAt.Should().BeNull();
        s.NextDueAt.Should().BeOnOrAfter(before.AddMonths(6)).And.BeOnOrBefore(after.AddMonths(6));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_non_positive_interval(int interval)
    {
        var act = () => NewSchedule(intervalMonths: interval);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // (2) matching completion advances LastPerformedAt/NextDueAt + resets ReminderSentAt.
    [Fact]
    public void RecordPerformed_advances_the_cycle_and_rearms_the_reminder()
    {
        var s = NewSchedule(intervalMonths: 12, lastPerformedAt: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        s.MarkReminderSent(DateTime.UtcNow);           // reminder already sent this cycle
        s.ReminderSentAt.Should().NotBeNull();

        var performed = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        s.RecordPerformed(performed);

        s.LastPerformedAt.Should().Be(performed);
        s.NextDueAt.Should().Be(performed.AddMonths(12)); // 2027-03-10
        s.ReminderSentAt.Should().BeNull("advancing the cycle must re-arm the reminder for the next due date");
    }

    // (3) reminder window: opens at NextDueAt − lead, once-guard, and off when inactive.
    [Fact]
    public void NeedsReminder_is_false_before_the_window_and_true_at_the_lead_boundary()
    {
        var last = DateTime.UtcNow.AddMonths(-12);      // → NextDueAt ≈ now
        var s = NewSchedule(intervalMonths: 12, leadDays: 14, lastPerformedAt: last);
        var due = s.NextDueAt;

        s.NeedsReminder(due.AddDays(-15)).Should().BeFalse("15 days out is before the 14-day lead window");
        s.NeedsReminder(due.AddDays(-14)).Should().BeTrue("the window opens exactly at NextDueAt − lead");
        s.NeedsReminder(due).Should().BeTrue();
    }

    [Fact]
    public void NeedsReminder_is_false_once_reminded_this_cycle()
    {
        var s = NewSchedule(intervalMonths: 12, leadDays: 14,
            lastPerformedAt: DateTime.UtcNow.AddMonths(-12));
        var atWindow = s.NextDueAt.AddDays(-1);

        s.NeedsReminder(atWindow).Should().BeTrue();
        s.MarkReminderSent(atWindow);
        s.NeedsReminder(atWindow).Should().BeFalse("the ReminderSentAt marker is a once-guard for the cycle");
    }

    [Fact]
    public void NeedsReminder_is_false_when_deactivated()
    {
        var s = NewSchedule(intervalMonths: 12, leadDays: 14,
            lastPerformedAt: DateTime.UtcNow.AddMonths(-12));
        s.Deactivate();
        s.NeedsReminder(s.NextDueAt).Should().BeFalse();
    }

    [Fact]
    public void UpdateSchedule_recomputes_NextDueAt_from_the_same_anchor_and_rearms()
    {
        var last = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var s = NewSchedule(intervalMonths: 12, leadDays: 14, lastPerformedAt: last);
        s.MarkReminderSent(DateTime.UtcNow);

        s.UpdateSchedule(recommendedIntervalMonths: 6, reminderLeadDays: 30, notes: "shorter cadence");

        s.RecommendedIntervalMonths.Should().Be(6);
        s.ReminderLeadDays.Should().Be(30);
        s.NextDueAt.Should().Be(last.AddMonths(6)); // anchor = LastPerformedAt
        s.ReminderSentAt.Should().BeNull();
        s.Notes.Should().Be("shorter cadence");
    }
}
