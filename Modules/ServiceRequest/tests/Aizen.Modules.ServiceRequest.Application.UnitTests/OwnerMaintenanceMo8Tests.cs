using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Command.Maintenance;
using Aizen.Modules.ServiceRequest.Application.Query.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-MO8 — owner maintenance self-service. The owner lists only their OWN schedules (OwnerUserId-scoped), the
/// owner set-active is owner-gated (a schedule the caller does not own is a clean not-found) while keeping the S12
/// idempotent + reactivate-conflict behaviour, and the reused upsert is idempotent by (vessel, category, type) and
/// stamps the owner passed by the owner controller (which sets it from the token, never the body). Uses an in-memory
/// fake repository mirroring the EF filters (the SR test project has no mocking library).
/// </summary>
public sealed class OwnerMaintenanceMo8Tests
{
    private sealed class FakeRepo : IMaintenanceScheduleRepository
    {
        private readonly List<MaintenanceScheduleEntity> _rows;
        public int SaveChangesCalls { get; private set; }
        public FakeRepo(params MaintenanceScheduleEntity[] rows) => _rows = rows.ToList();

        public Task<MaintenanceScheduleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<MaintenanceScheduleEntity?> GetActiveByKeyAsync(
            long vesselId, string cat, string? type, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x =>
                x.IsActive && !x.IsDeleted && x.VesselId == vesselId && x.ServiceCategoryCode == cat && x.ServiceTypeCode == type));

        public Task<MaintenanceScheduleEntity?> GetActiveMatchForCompletionAsync(
            long vesselId, string cat, string? type, CancellationToken ct = default)
            => Task.FromResult(_rows.FirstOrDefault(x =>
                x.IsActive && !x.IsDeleted && x.VesselId == vesselId && x.ServiceCategoryCode == cat
                && (x.ServiceTypeCode == type || x.ServiceTypeCode == null)));

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> GetDueForReminderAsync(
            DateTime nowUtc, int maxBatch, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => x.IsActive && !x.IsDeleted && x.ReminderSentAt == null)
                .OrderBy(x => x.NextDueAt).Take(maxBatch).Where(x => x.NeedsReminder(nowUtc)).ToList());

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> ListAsync(
            long? vesselId, bool includeInactive, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => !x.IsDeleted && (includeInactive || x.IsActive) && (vesselId == null || x.VesselId == vesselId))
                .OrderByDescending(x => x.IsActive).ThenBy(x => x.NextDueAt).ToList());

        public Task<IReadOnlyList<MaintenanceScheduleEntity>> ListByOwnerAsync(
            long ownerUserId, long? vesselId, bool includeInactive, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<MaintenanceScheduleEntity>)_rows
                .Where(x => !x.IsDeleted && x.OwnerUserId == ownerUserId
                         && (includeInactive || x.IsActive) && (vesselId == null || x.VesselId == vesselId))
                .OrderByDescending(x => x.IsActive).ThenBy(x => x.NextDueAt).ToList());

        public Task AddAsync(MaintenanceScheduleEntity entity, CancellationToken ct = default) { _rows.Add(entity); return Task.CompletedTask; }
        public void Update(MaintenanceScheduleEntity entity) { }
        public Task SaveChangesAsync(CancellationToken ct = default) { SaveChangesCalls++; return Task.CompletedTask; }
    }

    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    private static MaintenanceScheduleEntity Schedule(long id, long owner, long vessel = 10, bool active = true, string? type = null)
    {
        var s = MaintenanceScheduleEntity.Create(
            vesselId: vessel, serviceCategoryCode: "ENGINE", serviceTypeCode: type, ownerUserId: owner,
            recommendedIntervalMonths: 12, reminderLeadDays: 14, lastPerformedAt: DateTime.UtcNow.AddMonths(-6));
        s.Id = id;
        if (!active) s.Deactivate();
        return s;
    }

    // (1) owner lists ONLY their own schedules.
    [Fact]
    public async Task Owner_list_returns_only_own_schedules()
    {
        var repo = new FakeRepo(
            Schedule(1, owner: 100, vessel: 10),
            Schedule(2, owner: 100, vessel: 11, type: "OIL"),
            Schedule(3, owner: 200, vessel: 20));
        var handler = new GetOwnerMaintenanceSchedulesQueryHandler(repo);

        var mine = await handler.Handle(new GetOwnerMaintenanceSchedulesQuery(ownerUserId: 100), default);
        mine!.Schedules.Should().HaveCount(2);
        mine.Schedules.Should().OnlyContain(x => x.OwnerUserId == 100);

        var theirs = await handler.Handle(new GetOwnerMaintenanceSchedulesQuery(ownerUserId: 200), default);
        theirs!.Schedules.Should().ContainSingle().Which.OwnerUserId.Should().Be(200);
    }

    // (1b) no owner identity → empty (never fabricate).
    [Fact]
    public async Task Owner_list_with_no_identity_is_empty()
    {
        var handler = new GetOwnerMaintenanceSchedulesQueryHandler(new FakeRepo(Schedule(1, owner: 100)));
        var res = await handler.Handle(new GetOwnerMaintenanceSchedulesQuery(ownerUserId: 0), default);
        res!.Schedules.Should().BeEmpty();
    }

    // (2/6) set-active is owner-gated: the owner toggles their own; another owner's schedule is a clean not-found.
    [Fact]
    public async Task Owner_setactive_is_owner_gated()
    {
        var mine = Schedule(1, owner: 100, active: true);
        var repo = new FakeRepo(mine);
        var handler = new SetOwnerMaintenanceScheduleActiveCommandHandler(repo);

        // Owner 100 deactivates their own → ok.
        var ok = await handler.Handle(new SetOwnerMaintenanceScheduleActiveCommand(ownerUserId: 100, scheduleId: 1, isActive: false), default);
        ok!.IsActive.Should().BeFalse();
        mine.IsActive.Should().BeFalse();

        // Owner 200 cannot touch owner 100's schedule → clean not-found, no toggle.
        var foreign = () => handler.Handle(new SetOwnerMaintenanceScheduleActiveCommand(ownerUserId: 200, scheduleId: 1, isActive: true), default);
        (await foreign.Should().ThrowAsync<AizenBusinessException>()).WithMessage("*not found*");
        mine.IsActive.Should().BeFalse("a foreign caller must not reactivate it");
    }

    // (4) owner set-active keeps the S12 idempotent no-op + reactivate-conflict guard.
    [Fact]
    public async Task Owner_setactive_is_idempotent_and_guards_reactivate_conflict()
    {
        var active   = Schedule(1, owner: 100, active: true);   // owns (vessel 10, ENGINE, null)
        var inactive = Schedule(2, owner: 100, active: false);  // same key, off
        var repo = new FakeRepo(active, inactive);
        var handler = new SetOwnerMaintenanceScheduleActiveCommandHandler(repo);

        // Idempotent: already active → no write.
        await handler.Handle(new SetOwnerMaintenanceScheduleActiveCommand(100, 1, isActive: true), default);
        repo.SaveChangesCalls.Should().Be(0);

        // Reactivate #2 into a key #1 owns → clean business error, no write.
        var act = () => handler.Handle(new SetOwnerMaintenanceScheduleActiveCommand(100, 2, isActive: true), default);
        (await act.Should().ThrowAsync<AizenBusinessException>()).WithMessage("*already exists*");
        inactive.IsActive.Should().BeFalse();
        repo.SaveChangesCalls.Should().Be(0);
    }

    // (3) the reused upsert is idempotent by (vessel, category, type) and stamps the owner it is given (the owner
    // controller passes CurrentUserId — the token — here, never a body value).
    [Fact]
    public async Task Owner_upsert_is_idempotent_by_key_and_stamps_owner()
    {
        var repo = new FakeRepo();
        var handler = new UpsertMaintenanceScheduleCommandHandler(repo, EmptyConfig());

        UpsertMaintenanceScheduleRequest Req() => new()
        {
            VesselId = 10, OwnerUserId = 100, ServiceCategoryCode = "ENGINE", ServiceTypeCode = null,
            RecommendedIntervalMonths = 12, ReminderLeadDays = 14,
        };

        var first = await handler.Handle(new UpsertMaintenanceScheduleCommand(Req()), default);
        first!.Created.Should().BeTrue();

        var second = await handler.Handle(new UpsertMaintenanceScheduleCommand(Req()), default);
        second!.Created.Should().BeFalse("the same (vessel, category, type) updates the existing active row");
        second.ScheduleId.Should().Be(first.ScheduleId);

        var listed = await new GetOwnerMaintenanceSchedulesQueryHandler(repo)
            .Handle(new GetOwnerMaintenanceSchedulesQuery(ownerUserId: 100), default);
        listed!.Schedules.Should().ContainSingle().Which.OwnerUserId.Should().Be(100);
    }
}
