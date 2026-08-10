using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Application.Command.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Query.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// CLEANUP_S12_DEACTIVATE — the reachable active toggle: deactivate stops reminders, reactivate re-arms,
/// reactivating into a key another active schedule owns fails with a clean business error (no 500), set-active
/// is idempotent, and the admin list includes inactive schedules. Uses an in-memory fake repository (the SR
/// test project carries no mocking library) that mirrors the EF repository's filter/ordering semantics.
/// </summary>
public sealed class SetMaintenanceScheduleActiveHandlerTests
{
    // ── In-memory fake repository ──────────────────────────────────────────────────────────────────────
    private sealed class FakeRepo : IMaintenanceScheduleRepository
    {
        private readonly List<MaintenanceScheduleEntity> _rows;
        public int SaveChangesCalls { get; private set; }

        public FakeRepo(params MaintenanceScheduleEntity[] rows) => _rows = rows.ToList();

        public Task<MaintenanceScheduleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<MaintenanceScheduleEntity?> GetActiveByKeyAsync(
            long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x =>
                x.IsActive && !x.IsDeleted && x.VesselId == vesselId
                && x.ServiceCategoryCode == serviceCategoryCode && x.ServiceTypeCode == serviceTypeCode));

        public Task<MaintenanceScheduleEntity?> GetActiveMatchForCompletionAsync(
            long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x =>
                x.IsActive && !x.IsDeleted && x.VesselId == vesselId
                && x.ServiceCategoryCode == serviceCategoryCode
                && (x.ServiceTypeCode == serviceTypeCode || x.ServiceTypeCode == null)));

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> GetDueForReminderAsync(
            DateTime nowUtc, int maxBatch, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => x.IsActive && !x.IsDeleted && x.ReminderSentAt == null)
                .OrderBy(x => x.NextDueAt)
                .Take(maxBatch)
                .Where(x => x.NeedsReminder(nowUtc))
                .ToList());

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> ListAsync(
            long? vesselId, bool includeInactive, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => !x.IsDeleted && (includeInactive || x.IsActive)
                         && (vesselId == null || x.VesselId == vesselId))
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.NextDueAt)
                .ToList());

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> ListByOwnerAsync(
            long ownerUserId, long? vesselId, bool includeInactive, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => !x.IsDeleted && x.OwnerUserId == ownerUserId
                         && (includeInactive || x.IsActive)
                         && (vesselId == null || x.VesselId == vesselId))
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.NextDueAt)
                .ToList());

        public Task AddAsync(MaintenanceScheduleEntity entity, CancellationToken ct = default)
        {
            _rows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(MaintenanceScheduleEntity entity) { /* tracked in-place */ }
        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private static MaintenanceScheduleEntity Schedule(long id, bool active = true, string? type = null)
    {
        var s = MaintenanceScheduleEntity.Create(
            vesselId: 10, serviceCategoryCode: "ENGINE", serviceTypeCode: type, ownerUserId: 100,
            recommendedIntervalMonths: 12, reminderLeadDays: 14,
            lastPerformedAt: DateTime.UtcNow.AddMonths(-12)); // NextDueAt ≈ now → inside the reminder window
        s.Id = id;
        if (!active) s.Deactivate();
        return s;
    }

    // (1) deactivate → IsActive=false and the schedule no longer produces reminders.
    [Fact]
    public async Task Deactivate_turns_off_and_stops_reminders()
    {
        var s = Schedule(1, active: true);
        var repo = new FakeRepo(s);
        var handler = new SetMaintenanceScheduleActiveCommandHandler(repo);

        // Pre-condition: it WOULD be reminded while active.
        (await repo.GetDueForReminderAsync(DateTime.UtcNow, 100)).Should().ContainSingle(x => x.Id == 1);

        var result = await handler.Handle(new SetMaintenanceScheduleActiveCommand(1, isActive: false), default);

        result!.IsActive.Should().BeFalse();
        s.IsActive.Should().BeFalse();
        s.NeedsReminder(DateTime.UtcNow).Should().BeFalse();
        (await repo.GetDueForReminderAsync(DateTime.UtcNow, 100)).Should().BeEmpty("a deactivated schedule is not scanned");
        repo.SaveChangesCalls.Should().Be(1);
    }

    // (2) reactivate → back on.
    [Fact]
    public async Task Reactivate_turns_back_on()
    {
        var s = Schedule(1, active: false);
        var handler = new SetMaintenanceScheduleActiveCommandHandler(new FakeRepo(s));

        var result = await handler.Handle(new SetMaintenanceScheduleActiveCommand(1, isActive: true), default);

        result!.IsActive.Should().BeTrue();
        s.IsActive.Should().BeTrue();
    }

    // (3) reactivate into a key another ACTIVE schedule owns → clean business error, no 500, no write.
    [Fact]
    public async Task Reactivate_into_a_taken_key_fails_with_a_clean_business_error()
    {
        var active   = Schedule(1, active: true);   // owns (vessel 10, ENGINE, null)
        var inactive = Schedule(2, active: false);  // same key, currently off
        var repo = new FakeRepo(active, inactive);
        var handler = new SetMaintenanceScheduleActiveCommandHandler(repo);

        var act = () => handler.Handle(new SetMaintenanceScheduleActiveCommand(2, isActive: true), default);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .WithMessage("*already exists*");
        inactive.IsActive.Should().BeFalse("the conflicting reactivate must not have toggled the row");
        repo.SaveChangesCalls.Should().Be(0, "a refused reactivate writes nothing");
    }

    // (5) set-active is idempotent — toggling to the current state is a no-op (no write).
    [Fact]
    public async Task Setting_the_same_state_is_a_noop()
    {
        var s = Schedule(1, active: true);
        var repo = new FakeRepo(s);
        var handler = new SetMaintenanceScheduleActiveCommandHandler(repo);

        var result = await handler.Handle(new SetMaintenanceScheduleActiveCommand(1, isActive: true), default);

        result!.IsActive.Should().BeTrue();
        repo.SaveChangesCalls.Should().Be(0, "no state change → no write");
    }

    [Fact]
    public async Task Not_found_is_a_clean_business_error()
    {
        var handler = new SetMaintenanceScheduleActiveCommandHandler(new FakeRepo());

        var act = () => handler.Handle(new SetMaintenanceScheduleActiveCommand(999, isActive: false), default);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*not found*");
    }

    // (4) the admin list returns inactive schedules (default IncludeInactive = true), active first.
    [Fact]
    public async Task Admin_list_includes_inactive_schedules_by_default()
    {
        var active   = Schedule(1, active: true);
        var inactive = Schedule(2, active: false, type: "OIL_CHANGE");
        var repo = new FakeRepo(active, inactive);
        var handler = new GetMaintenanceScheduleListQueryHandler(repo);

        var response = await handler.Handle(new GetMaintenanceScheduleListQuery(vesselId: null), default);

        response!.Schedules.Should().HaveCount(2);
        response.Schedules.Should().Contain(x => x.Id == 2 && !x.IsActive);
        response.Schedules[0].IsActive.Should().BeTrue("active schedules are ordered first");
    }

    [Fact]
    public async Task Admin_list_can_exclude_inactive_when_requested()
    {
        var active   = Schedule(1, active: true);
        var inactive = Schedule(2, active: false, type: "OIL_CHANGE");
        var handler = new GetMaintenanceScheduleListQueryHandler(new FakeRepo(active, inactive));

        var response = await handler.Handle(
            new GetMaintenanceScheduleListQuery(vesselId: null, includeInactive: false), default);

        response!.Schedules.Should().ContainSingle().Which.Id.Should().Be(1);
    }
}
