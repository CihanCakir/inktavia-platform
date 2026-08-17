using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryKitLifecycleEventRepository : ICargoDryKitLifecycleEventRepository
{
    private readonly CargoDryDbContext _db;

    public CargoDryKitLifecycleEventRepository(CargoDryDbContext db)
        => _db = db;

    public async Task<List<CargoDryKitLifecycleEventEntity>> GetByKitIdAsync(
        long kitId, CancellationToken ct = default)
        => await _db.KitLifecycleEvents
            .Where(e => e.KitId == kitId && e.IsActive)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync(ct);

    public async Task<(List<CargoDryKitLifecycleEventEntity> Items, int Total)> GetPagedAsync(
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
        CancellationToken             ct = default)
    {
        var q = _db.KitLifecycleEvents.Where(e => e.IsActive);

        if (kitId.HasValue)
            q = q.Where(e => e.KitId == kitId.Value);

        if (!string.IsNullOrWhiteSpace(kitCode))
            q = q.Where(e => e.KitCode == kitCode);

        if (!string.IsNullOrWhiteSpace(batchCode))
            q = q.Where(e => e.BatchCode == batchCode);

        if (!string.IsNullOrWhiteSpace(productCode))
            q = q.Where(e => e.ProductCode == productCode);

        if (eventType.HasValue)
            q = q.Where(e => e.EventType == eventType.Value);

        if (actorUserId.HasValue)
            q = q.Where(e => e.ActorUserId == actorUserId.Value);

        if (dateFrom.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= dateTo.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountRecentAsync(int hours = 24, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-hours);
        return await _db.KitLifecycleEvents
            .CountAsync(e => e.IsActive && e.OccurredAtUtc >= since, ct);
    }

    public async Task AddAsync(CargoDryKitLifecycleEventEntity entity, CancellationToken ct = default)
        => await _db.KitLifecycleEvents.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
