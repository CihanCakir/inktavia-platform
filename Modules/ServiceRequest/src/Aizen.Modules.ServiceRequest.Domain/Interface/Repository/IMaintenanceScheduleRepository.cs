using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;

namespace Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

[DocumentationInfo("Maintenance schedule repository interface", "Data access for MaintenanceScheduleEntity (S12/N2).")]
public interface IMaintenanceScheduleRepository
{
    Task<MaintenanceScheduleEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>The active schedule for the exact unique key (VesselId, ServiceCategoryCode, ServiceTypeCode). Upsert key.</summary>
    Task<MaintenanceScheduleEntity?> GetActiveByKeyAsync(
        long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default);

    /// <summary>
    /// The active schedule a completed (vessel, category, type) service request advances. Prefers an exact type match,
    /// otherwise a category-level schedule (ServiceTypeCode is null — covers the whole category).
    /// </summary>
    Task<MaintenanceScheduleEntity?> GetActiveMatchForCompletionAsync(
        long vesselId, string serviceCategoryCode, string? serviceTypeCode, CancellationToken ct = default);

    /// <summary>N2 scan: active, not yet reminded this cycle, and inside the reminder window (NextDueAt ≤ cutoff).</summary>
    Task<IReadOnlyList<MaintenanceScheduleEntity>> GetDueForReminderAsync(
        DateTime nowUtc, int maxBatch, CancellationToken ct = default);

    /// <summary>
    /// Admin listing, optionally scoped to a vessel. When <paramref name="includeInactive"/> is true (the admin
    /// default) deactivated schedules are returned too — active first, then soonest-due — so a turned-off schedule
    /// stays visible and reactivatable. When false, only active schedules.
    /// </summary>
    Task<IReadOnlyList<MaintenanceScheduleEntity>> ListAsync(
        long? vesselId, bool includeInactive, CancellationToken ct = default);

    /// <summary>
    /// BE-MO8 — the caller-owner's own schedules (OwnerUserId-scoped), optionally narrowed to one vessel. Same
    /// active-first / soonest-due ordering + includeInactive semantics as <see cref="ListAsync"/>.
    /// </summary>
    Task<IReadOnlyList<MaintenanceScheduleEntity>> ListByOwnerAsync(
        long ownerUserId, long? vesselId, bool includeInactive, CancellationToken ct = default);

    Task AddAsync(MaintenanceScheduleEntity entity, CancellationToken ct = default);
    void Update(MaintenanceScheduleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
