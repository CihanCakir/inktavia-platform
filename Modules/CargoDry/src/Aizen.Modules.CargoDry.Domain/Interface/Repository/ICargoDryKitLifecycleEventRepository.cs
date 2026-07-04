using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryKitLifecycleEventRepository
{
    /// <summary>
    /// Returns all lifecycle events for a single kit, ordered newest-first.
    /// </summary>
    Task<List<CargoDryKitLifecycleEventEntity>> GetByKitIdAsync(
        long kitId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged, filtered list of lifecycle events across all kits.
    /// </summary>
    Task<(List<CargoDryKitLifecycleEventEntity> Items, int Total)> GetPagedAsync(
        long?                         kitId,
        string?                       kitCode,
        string?                       batchCode,
        string?                       productCode,
        CargoDryKitLifecycleEventType? eventType,
        long?                         actorUserId,
        DateTimeOffset?               dateFrom,
        DateTimeOffset?               dateTo,
        int                           skip,
        int                           take,
        CancellationToken             ct = default);

    /// <summary>
    /// Returns the count of lifecycle events that occurred in the past <paramref name="hours"/> hours.
    /// Used for the operational overview dashboard KPI.
    /// </summary>
    Task<int> CountRecentAsync(int hours = 24, CancellationToken ct = default);

    /// <summary>
    /// Stages a new event for persistence. Caller is responsible for SaveChangesAsync.
    /// </summary>
    Task AddAsync(CargoDryKitLifecycleEventEntity entity, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
