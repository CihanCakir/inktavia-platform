using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("Maintenance schedule repository", "EF Core implementation of IMaintenanceScheduleRepository.")]
public sealed class MaintenanceScheduleRepository : IMaintenanceScheduleRepository
{
    private readonly ServiceRequestDbContext _db;

    public MaintenanceScheduleRepository(ServiceRequestDbContext db) => _db = db;

    public Task<MaintenanceScheduleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<MaintenanceScheduleEntity?> GetActiveByKeyAsync(
        long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default)
        => _db.MaintenanceSchedules.FirstOrDefaultAsync(
            x => x.IsActive && !x.IsDeleted
              && x.VesselId == vesselId
              && x.ServiceCategoryCode == serviceCategoryCode
              && x.ServiceTypeCode == serviceTypeCode,
            ct);

    public async Task<MaintenanceScheduleEntity?> GetActiveMatchForCompletionAsync(
        long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default)
    {
        // Exact (vessel, category, type) match takes precedence; otherwise a category-level schedule (null type).
        var candidates = await _db.MaintenanceSchedules
            .Where(x => x.IsActive && !x.IsDeleted
                     && x.VesselId == vesselId
                     && x.ServiceCategoryCode == serviceCategoryCode
                     && (x.ServiceTypeCode == serviceTypeCode || x.ServiceTypeCode == null))
            .ToListAsync(ct);

        return candidates.FirstOrDefault(x => x.ServiceTypeCode == serviceTypeCode)
            ?? candidates.FirstOrDefault(x => x.ServiceTypeCode == null);
    }

    public async Task<IReadOnlyList<MaintenanceScheduleEntity>> GetDueForReminderAsync(
        DateTime nowUtc, int maxBatch, CancellationToken ct = default)
    {
        // Translation-safe DB pre-filter: active, un-armed, soonest-due first. In-window rows (now ≥ NextDueAt − lead)
        // have the smallest NextDueAt so they sort first and are captured by the batch. The exact per-row window
        // (leads are per-schedule) is applied in memory via the unit-tested NeedsReminder — no fragile AddDays(column) SQL.
        var candidates = await _db.MaintenanceSchedules
            .Where(x => x.IsActive && !x.IsDeleted && x.ReminderSentAt == null)
            .OrderBy(x => x.NextDueAt)
            .Take(maxBatch)
            .ToListAsync(ct);

        return candidates.Where(x => x.NeedsReminder(nowUtc)).ToList();
    }

    public async Task<IReadOnlyList<MaintenanceScheduleEntity>> ListAsync(
        long? vesselId, bool includeInactive, CancellationToken ct = default)
        => await _db.MaintenanceSchedules
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                     && (includeInactive || x.IsActive)
                     && (vesselId == null || x.VesselId == vesselId))
            .OrderByDescending(x => x.IsActive) // active first, then soonest-due — inactive rows sink to the bottom
            .ThenBy(x => x.NextDueAt)
            .ToListAsync(ct);

    public Task AddAsync(MaintenanceScheduleEntity entity, CancellationToken ct = default)
        => _db.MaintenanceSchedules.AddAsync(entity, ct).AsTask();

    public void Update(MaintenanceScheduleEntity entity) => _db.MaintenanceSchedules.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
